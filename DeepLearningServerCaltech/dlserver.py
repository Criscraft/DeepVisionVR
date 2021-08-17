import zmq
import json
import base64
import cv2
import torch.nn.functional as F 
import numpy as np
from DLPerformer import DLPerformer


context = zmq.Context()
socket = context.socket(zmq.REP)
socket.bind("tcp://*:5570")
dl_performer = DLPerformer()
print("DLserver ready")


class Transform(object):

    def __init__(self):
        self.v_max = 0
        self.v_min = 0

    def transform(self, data, fitting):
        if fitting:
            self.v_max = out.max()
            self.v_min = out.min()
            #self.v_max = np.percentile(out, 98)
            #self.v_min = np.percentile(out, 2)
        stretch_constant = 10
        data = (data - self.v_min) / (self.v_max - self.v_min + 1e-6) * stretch_constant
        data = np.log(data + 1) / np.log(stretch_constant + 1) * 255
        data = data.clip(0, 255)
        return data


transform_instance = Transform()

while True:
    #  Wait for next request from client
    message = socket.recv_multipart()
    print("Received request: %s" % message)

    if message[0]==b'RequestDataOverview':
        out = dl_performer.get_data_overview()
        json_obj = json.dumps(out, indent=1).encode('utf-8')
        socket.send_multipart([b'RequestDataOverview', json_obj])
        print("send RequestDataOverview")

    elif message[0]==b'RequestDatasetImage':
        index = int(message[1])
        out = dl_performer.get_data_item(index)
        image = out['data'].numpy()
        image = image * 255
        image = image.astype("uint8")
        image = image[np.array([2,1,0])]
        image = image.transpose([1,2,0])
        _, buffer = cv2.imencode('.png', image)
        image_enc = base64.b64encode(buffer)
        label = str(out['label']).encode('utf-8')
        socket.send_multipart([b'RequestDatasetImage', str(index).encode('utf-8'), image_enc, label])
        print("send RequestDatasetImage")
    
    elif message[0]==b'RequestPrepareForInput':
        dl_performer.prepare_for_input(int(message[1]))
        socket.send_multipart([b'RequestPrepareForInput'])
        print("send RequestPrepareForInput")

    elif message[0]==b'RequestNetworkArchitecture':
        out = dl_performer.get_architecture()
        out = [json.dumps(item, indent=1).encode('utf-8') for item in out]
        socket.send_multipart([b'RequestNetworkArchitecture'] + out)

    elif message[0]==b'RequestLayerActivation':
        layer_id = int(message[1])
        out = dl_performer.get_activation(layer_id)
        # give first element of activations because we do not want to have the batch dimension
        out = out[0].cpu().numpy()

        out = transform_instance.transform(out, True)
        zero_value = transform_instance.transform(0., False)
        out = out.astype("uint8")
        # if the layer has 1D data, make a 2D image out of it
        if len(out.shape) == 1:
            out = out[None, None, ...]
        out_cv2 = []
        for item in out:
            #_, buffer = cv2.imencode('.bmp', item) # does not work anymore!?
            _, buffer = cv2.imencode('.png', item)
            out_cv2.append(buffer)
        socket.send_multipart([b'RequestLayerActivation', str(layer_id).encode('utf-8'), str(zero_value).encode('utf-8')] + [base64.b64encode(item) for item in out_cv2])
        print("send RequestLayerActivation")

    elif message[0]==b'RequestClassificationResult':
        out = dl_performer.get_activation(len(dl_performer.features) - 1)
        out = F.softmax(out, 1) * 100
        out = out[0].cpu().numpy()
        indices_tmp = np.argsort(-out)
        indices_tmp = indices_tmp[:16]
        out = out[indices_tmp]
        out_dict = {
            'class_indices' : [f'{item}' for item in indices_tmp], 
            'confidence_values' : [f'{item:.2f}' for item in out],
        }
        socket.send_multipart([b'RequestClassificationResult', json.dumps(out_dict, indent=1).encode('utf-8')])
        print("send RequestClassificationResult")
    
    elif message[0]==b'RequestWeightHistogram':
        layer_id = int(message[1])
        weights = dl_performer.get_weights(layer_id)
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
        socket.send_multipart([b'RequestWeightHistogram', str(layer_id).encode('utf-8'), json.dumps(out_dict, indent=1).encode('utf-8')])
        print("send RequestWeightHistogram")

    elif message[0]==b'RequestActivationHistogram':
        layer_id = int(message[1])
        hist, bins = np.histogram(dl_performer.get_activation(layer_id).cpu().numpy(), 16)
        bins = 0.5 * (bins[1:] + bins[:-1])
        out_dict = {
            'counts' : [f'{item}' for item in hist], 
            'bins' : [f'{item}' for item in bins],
        }
        socket.send_multipart([b'RequestActivationHistogram', str(layer_id).encode('utf-8'), json.dumps(out_dict, indent=1).encode('utf-8')])
        print("send RequestActivationHistogram")

    else:
        raise ValueError("Could not process the message.")