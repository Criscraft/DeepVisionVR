import json
import time
import numpy as np
import run_server_caltech
from ActivationImage import ActivationImage


dl_performer = run_server_caltech.get_dl_performer()


class NetworkResource:

    def on_get(self, req, resp):
        out = dl_performer.get_architecture()
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print("send NetworkResource")


class NetworkActivationImageResource:

    def on_get(self, req, resp, layerid : int):
        tensor = dl_performer.get_activation(layerid)
        # give first element of activations because we do not want to have the batch dimension
        tensor = tensor[0]
        data = dl_performer.tensor_to_uint_transform(tensor, True)
        zero_value = dl_performer.tensor_to_uint_transform(0., False)
        
        # if the layer has 1D data, make a 2D image out of it
        if len(data.shape) == 1:
            data = data[None, None, ...]

        tensors = [dl_performer.encode_image(ten) for ten in data]
        out = {
            "tensors" : tensors,
            "layerID" : layerid,
            "zeroValue" : int(zero_value),
            "mode" : "Activation",
            }
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print(f"send NetworkActivationImageResource {layerid}")


class NetworkFeatureVisualizationResource:

    def on_get(self, req, resp, layerid : int):
        data = dl_performer.get_feature_visualization(layerid)
        data = data[:,np.array([2, 1, 0])] # for sorting color channels
        data = data.transpose([0, 2, 3, 1]) # put channel dimension to last
        tensors = [dl_performer.encode_image(ten) for ten in data]
        out = {
            "tensors" : tensors,
            "layerID" : layerid,
            "mode" : "FeatureVisualization",
            }
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print(f"send NetworkFeatureVisualizationResource {layerid}")


class NetworkPrepareForInputResource:

    def on_put(self, req, resp):
        jsonfile = json.load(req.stream)
        activation_image = ActivationImage(
            image_ID = jsonfile["imageID"],
            layer_ID = jsonfile["layerID"],
            channel_ID = jsonfile["channelID"],
            mode = ActivationImage.Mode(jsonfile["mode"]))
        dl_performer.prepare_for_input(activation_image)
        print("send NetworkPrepareForInputResource")


class NetworkClassificationResultResource:

    def on_get(self, req, resp):
        posteriors, class_indices = dl_performer.get_classification_result()
        out = {
            'class_indices' : [f'{item}' for item in class_indices], 
            'confidence_values' : [f'{item:.2f}' for item in posteriors],
        }
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print("send NetworkClassificationResultResource")


class NetworkWeightHistogramResource:

    def on_get(self, req, resp, layerid : int):
        weights = dl_performer.get_weights(layerid)
        has_weights = weights is not None
        if has_weights:
            hist, bins = np.histogram(weights.detach().cpu().numpy(), 16)
            bins = 0.5 * (bins[1:] + bins[:-1])
        else:
            hist, bins = [], []
        out = {
            'layer_id' : layerid,
            'has_weights' : str(has_weights),
            'counts' : [f'{item}' for item in hist], 
            'bins' : [f'{item}' for item in bins],
        }
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print(f"send NetworkWeightHistogramResource {layerid}")


class NetworkActivationHistogramResource:

    def on_get(self, req, resp, layerid : int):
        activations = dl_performer.get_activation(layerid)
        hist, bins = np.histogram(activations.detach().cpu().numpy(), 16)
        bins = 0.5 * (bins[1:] + bins[:-1])
        out = {
            'layer_id' : layerid,
            'counts' : [f'{item}' for item in hist], 
            'bins' : [f'{item}' for item in bins],
        }
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print(f"send NetworkActivationHistogramResource {layerid}")

        
class DataImagesResource:

    def on_get(self, req, resp):
        out = dl_performer.get_data_overview()
        tensors = []
        labels = []
        for i in range(out["len"]):
            tensor, label = dl_performer.get_data_item(i)
            tensors.append(dl_performer.tensor_to_string(tensor))
            labels.append(label)
        out['tensors'] = tensors
        out['label_ids'] = labels
        out["class_names"] = dl_performer.dataset.class_names
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print("send DataImagesResource")


class DataNoiseImageResource:

    def on_get(self, req, resp):
        image = dl_performer.generate_noise_image()
        image = image.numpy()
        image = image * 255
        image = image.astype("uint8")
        image = image[np.array([2,1,0])] # for sorting color channels
        image = image.transpose([1,2,0]) # put channel dimension to last
        image_enc = dl_performer.encode_image(image)
        out = {'tensor' : image_enc}
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print("send DataNoiseImageResource")


        







class TestShortResource:

    def on_get(self, req, resp):
        doc = {
            'Hello': [
                {
                    'World': 'Hip Huepf'
                }
            ]
        }
        resp.text = json.dumps(doc, ensure_ascii=False)


class TestLongResource:

    def on_get(self, req, resp):
        time.sleep(60)
        doc = {
            'Hello': [
                {
                    'World': 'Hip Huepf'
                }
            ]
        }
        resp.text = json.dumps(doc, ensure_ascii=False)