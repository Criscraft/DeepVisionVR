import zmq
import json
import base64
import cv2
import torch.nn.functional as F 
import numpy as np
from ActivationImage import ActivationImage


class Transform(object):

    def __init__(self):
        self.v_max = 0
        self.v_min = 0

    def transform(self, data, fitting):
        if fitting:
            self.v_max = data.max()
            self.v_min = data.min()
            #self.v_max = np.percentile(out, 98)
            #self.v_min = np.percentile(out, 2)

        # boost dark regions
        stretch_constant = 10
        data = (data - self.v_min) / (self.v_max - self.v_min + 1e-6) * stretch_constant
        data = np.log(data + 1) / np.log(stretch_constant + 1) * 255
        
        data = data.clip(0, 255)
        return data

class DLServer(object):

    def __init__(self, dl_performer):
        self.transform_instance = Transform()
        self.context = zmq.Context()
        self.socket = self.context.socket(zmq.REP)
        self.socket.bind("tcp://*:5570")
        self.dl_performer = dl_performer
        print("DLserver ready")

    def run(self):
        while True:
            #  Wait for next request from client
            message = self.socket.recv_multipart()
            print("Received request: %s" % message)

            if message[0]==b'RequestDataOverview':
                out = self.dl_performer.get_data_overview()
                json_obj = json.dumps(out, indent=1).encode('utf-8')
                self.socket.send_multipart([b'RequestDataOverview', json_obj])
                print("send RequestDataOverview")

            elif message[0]==b'RequestDatasetImage':
                index = int(message[1])
                out = self.dl_performer.get_data_item(index)
                image = out['data'].numpy()
                image = image * 255
                image = image.astype("uint8")
                image = image[np.array([2,1,0])] # for sorting color channels
                image = image.transpose([1,2,0]) # put channel dimension to last
                _, buffer = cv2.imencode('.png', image)
                image_enc = base64.b64encode(buffer)
                label = str(out['label']).encode('utf-8')
                self.socket.send_multipart([b'RequestDatasetImage', str(index).encode('utf-8'), image_enc, label])
                print("send RequestDatasetImage")
            
            elif message[0]==b'RequestPrepareForInput':
                mode_string = message[4]
                if mode_string == b'DatasetImage': mode = ActivationImage.Mode.DatasetImage
                elif mode_string == b'FeatureVisualization': mode = ActivationImage.Mode.FeatureVisualization
                elif mode_string == b'NoiseImage': mode = ActivationImage.Mode.NoiseImage
                else: mode = ActivationImage.Mode.Unknown
                
                activation_image = ActivationImage(
                    image_ID = int(message[1]),
                    layer_ID = int(message[2]),
                    channel_ID = int(message[3]),
                    mode = mode)

                self.dl_performer.prepare_for_input(activation_image)
                self.socket.send_multipart([b'RequestPrepareForInput'])
                print("send RequestPrepareForInput")

            elif message[0]==b'RequestNetworkArchitecture':
                out = self.dl_performer.get_architecture()
                out = [json.dumps(item, indent=1).encode('utf-8') for item in out]
                self.socket.send_multipart([b'RequestNetworkArchitecture'] + out)

            elif message[0]==b'RequestLayerActivation':
                layer_id = int(message[1])
                out = self.dl_performer.get_activation(layer_id)
                # give first element of activations because we do not want to have the batch dimension
                out = out[0].cpu().numpy()

                out = self.transform_instance.transform(out, True)
                zero_value = self.transform_instance.transform(0., False)
                out = out.astype("uint8")
                # if the layer has 1D data, make a 2D image out of it
                if len(out.shape) == 1:
                    out = out[None, None, ...]
                out_cv2 = []
                for item in out:
                    #_, buffer = cv2.imencode('.bmp', item) # does not work anymore!?
                    _, buffer = cv2.imencode('.png', item)
                    out_cv2.append(buffer)
                self.socket.send_multipart([b'RequestLayerActivation', str(layer_id).encode('utf-8'), str(zero_value).encode('utf-8')] + [base64.b64encode(item) for item in out_cv2])
                print("send RequestLayerActivation")

            elif message[0]==b'RequestLayerFeatureVisualization':
                layer_id = int(message[1])
                out = self.dl_performer.get_feature_visualization(layer_id)
                out = out[:,np.array([2, 1, 0])] # for sorting color channels
                out = out.transpose([0, 2, 3, 1]) # put channel dimension to last
                print(out.shape)
                print(out.__class__)
                print(out.min())
                print(out.max())
                out_cv2 = []
                for item in out:
                    #_, buffer = cv2.imencode('.bmp', item) # does not work anymore!?
                    _, buffer = cv2.imencode('.png', item)
                    out_cv2.append(buffer)
                message_tracker = self.socket.send_multipart([b'RequestLayerFeatureVisualization', str(layer_id).encode('utf-8')] + [base64.b64encode(item) for item in out_cv2])
                print("send RequestLayerFeatureVisualization")


            elif message[0]==b'RequestClassificationResult':
                out = self.dl_performer.get_activation(len(self.dl_performer.features) - 1)
                out = F.softmax(out, 1) * 100
                out = out[0].cpu().numpy()
                indices_tmp = np.argsort(-out)
                indices_tmp = indices_tmp[:16]
                out = out[indices_tmp]
                out_dict = {
                    'class_indices' : [f'{item}' for item in indices_tmp], 
                    'confidence_values' : [f'{item:.2f}' for item in out],
                }
                self.socket.send_multipart([b'RequestClassificationResult', json.dumps(out_dict, indent=1).encode('utf-8')])
                print("send RequestClassificationResult")
            
            elif message[0]==b'RequestWeightHistogram':
                layer_id = int(message[1])
                weights = self.dl_performer.get_weights(layer_id)
                has_weights = weights is not None
                if has_weights:
                    hist, bins = np.histogram(weights.detach().cpu().numpy(), 16)
                    bins = 0.5 * (bins[1:] + bins[:-1])
                else:
                    hist, bins = [], []
                out_dict = {
                    'has_weights' : str(has_weights),
                    'counts' : [f'{item}' for item in hist], 
                    'bins' : [f'{item}' for item in bins],
                }
                self.socket.send_multipart([b'RequestWeightHistogram', str(layer_id).encode('utf-8'), json.dumps(out_dict, indent=1).encode('utf-8')])
                print("send RequestWeightHistogram")

            elif message[0]==b'RequestActivationHistogram':
                layer_id = int(message[1])
                hist, bins = np.histogram(self.dl_performer.get_activation(layer_id).cpu().numpy(), 16)
                bins = 0.5 * (bins[1:] + bins[:-1])
                out_dict = {
                    'counts' : [f'{item}' for item in hist], 
                    'bins' : [f'{item}' for item in bins],
                }
                self.socket.send_multipart([b'RequestActivationHistogram', str(layer_id).encode('utf-8'), json.dumps(out_dict, indent=1).encode('utf-8')])
                print("send RequestActivationHistogram")
            

            elif message[0]==b'RequestNoiseImage':
                out = self.dl_performer.generate_noise_image()
                image = out.numpy()
                image = image * 255
                image = image.astype("uint8")
                image = image[np.array([2,1,0])] # for sorting color channels
                image = image.transpose([1,2,0]) # put channel dimension to last
                _, buffer = cv2.imencode('.png', image)
                image_enc = base64.b64encode(buffer)
                self.socket.send_multipart([b'RequestNoiseImage', image_enc])
                print("send RequestNoiseImage")

            else:
                raise ValueError("Could not process the message.")