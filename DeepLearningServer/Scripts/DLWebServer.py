import json
import time
import torch
import numpy as np
import falcon
import Scripts.utils as utils
from Scripts.DLNetwork import FeatureVisualizationMode
from Scripts.ActivationImage import ActivationImage

# Create global variables. They will be filled inplace.
networks = []
datasets = []
noise_generators = []


def select_project_server(source):
    networks.extend(source.get_dl_networks())
    datasets.extend(source.get_datasets())
    noise_generators.extend(source.get_noise_generators())

    for network in networks:
        dataset = datasets[0]
        data = torch.zeros(dataset.get_data_item(0, True)[0].shape)
        activation_image = ActivationImage(image_ID=0, mode=ActivationImage.Mode.DatasetImage, data=data)
        network.prepare_for_input(activation_image)


class NetworkResource:

    def on_get(self, req, resp):
        out = {
            "nnetworks" : len(networks),
            "ndatasets" : len(datasets),
            "nnoiseGenerators" : len(noise_generators),
        }
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print("send NetworkResource")


class NetworkArchitectureResource:

    def on_get(self, req, resp, networkid):
        out = networks[networkid].get_architecture()
        out["networkid"] = networkid
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print("send NetworkArchitectureResource for network {networkid}")


class NetworkActivationImageResource:

    def on_get(self, req, resp, networkid : int, layerid : int):
        network = networks[networkid]
        tensor = network.get_activation(layerid)
        # give first element of activations because we do not want to have the batch dimension
        tensor = tensor[0]
        tensor_to_uint_transform = utils.TransformToUint()
        data = tensor_to_uint_transform(tensor, True)
        zero_value = tensor_to_uint_transform(0., False)
        
        # if the layer has 1D data, make a 2D image out of it
        if len(data.shape) == 1:
            data = data.reshape((-1, 1, 1))

        tensors = [utils.encode_image(ten) for ten in data]
        out = {
            "tensors" : tensors,
            "networkid" : networkid,
            "layerID" : layerid,
            "zeroValue" : int(zero_value),
            "mode" : "Activation",
            }
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print(f"send NetworkActivationImageResource for network {networkid} layer {layerid}")


class NetworkFeatureVisualizationResource:

    def on_get(self, req, resp, networkid : int, layerid : int):
        network = networks[networkid]
        data = network.get_feature_visualization(layerid)
        data = data[:,np.array([2, 1, 0])] # for sorting color channels
        data = data.transpose([0, 2, 3, 1]) # put channel dimension to last
        tensors = [utils.encode_image(ten) for ten in data]
        out = {
            "tensors" : tensors,
            "networkid" : networkid,
            "layerID" : layerid,
            "mode" : "FeatureVisualization",
            }
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print(f"send NetworkFeatureVisualizationResource for network {networkid} layer {layerid}")


class NetworkPrepareForInputResource:

    def on_put(self, req, resp, networkid : int):
        jsonfile = json.load(req.stream)
        activation_image = ActivationImage(
            network_ID = jsonfile["networkID"],
            dataset_ID = jsonfile["datasetID"],
            image_ID = jsonfile["imageID"],
            layer_ID = jsonfile["layerID"],
            channel_ID = jsonfile["channelID"],
            noise_generator_ID = jsonfile["noiseGeneratorID"],
            mode = ActivationImage.Mode(jsonfile["mode"]))
        
        network = networks[networkid]

        image = None
        if activation_image.mode == ActivationImage.Mode.DatasetImage and activation_image.image_ID >= 0:
            image = datasets[activation_image.dataset_ID].get_data_item(activation_image.image_ID, True)[0]
        elif activation_image.mode == ActivationImage.Mode.FeatureVisualization:
            image = networks[activation_image.network_ID].try_load_feature_visualization(activation_image.layer_ID)
            if image is not None:
                image = image[activation_image.channel_ID]
            else:
                raise falcon.HTTPBadRequest(title="Feature Visualization is not yet produced.")
        elif activation_image.mode == ActivationImage.Mode.NoiseImage:
            image = noise_generators[activation_image.noise_generator_ID].get_noise_image()
        elif activation_image.mode == ActivationImage.Mode.Activation:
            raise falcon.HTTPBadRequest(title="You cannot load an activation")
        else:
            image = torch.zeros(dataset.get_data_item(0, True)[0].shape)
        activation_image.data = image

        network.prepare_for_input(activation_image)
        print(f"send NetworkPrepareForInputResource for network {networkid}")


class NetworkClassificationResultResource:

    def on_get(self, req, resp, networkid : int):
        network = networks[networkid]
        dataset = datasets[network.active_data_item.dataset_ID]
        class_names = dataset.class_names
        posteriors, class_indices = network.get_classification_result()
        out = {
            "networkid" : networkid,
            "class_names" : list(class_names[class_indices]), 
            "confidence_values" : [f"{item:.2f}" for item in posteriors],

        }
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print(f"send NetworkClassificationResultResource for network {networkid}")


class NetworkWeightHistogramResource:

    def on_get(self, req, resp, networkid : int, layerid : int):
        weights = networks[networkid].get_weights(layerid)
        has_weights = weights is not None
        if has_weights:
            hist, bins = np.histogram(weights.detach().cpu().numpy(), 16)
            bins = 0.5 * (bins[1:] + bins[:-1])
        else:
            hist, bins = [], []
        out = {
            "networkid" : networkid,
            "layer_id" : layerid,
            "has_weights" : str(has_weights),
            "counts" : [f"{item}" for item in hist], 
            "bins" : [f"{item}" for item in bins],
        }
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print(f"send NetworkWeightHistogramResource for network {networkid} layer {layerid}")


class NetworkActivationHistogramResource:

    def on_get(self, req, resp, networkid : int, layerid : int):
        activations = networks[networkid].get_activation(layerid)
        hist, bins = np.histogram(activations.detach().cpu().numpy(), 16)
        bins = 0.5 * (bins[1:] + bins[:-1])
        out = {
            "networkid" : networkid,
            "layer_id" : layerid,
            "counts" : [f"{item}" for item in hist], 
            "bins" : [f"{item}" for item in bins],
        }
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print(f"send NetworkActivationHistogramResource for network {networkid} layer {layerid}")

        
class DataImagesResource:

    def on_get(self, req, resp, datasetid : int):
        dataset = datasets[datasetid]
        out = dataset.get_data_overview()
        tensors = []
        labels = []
        for i in range(out["len"]):
            tensor, label = dataset.get_data_item(i, False)
            tensors.append(utils.tensor_to_string(tensor))
            labels.append(label)
        out['tensors'] = tensors
        out['label_ids'] = labels
        out["class_names"] = list(dataset.class_names)
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print("send DataImagesResource for dataset {datasetid}")


class DataNoiseImageResource:

    def on_get(self, req, resp, noiseid : int):
        noise_generator = noise_generators[noiseid]
        noise_generator.generate_noise_image()
        image = noise_generator.get_noise_image()
        image_enc = utils.tensor_to_string(image)
        out = {'tensor' : image_enc}
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print("send DataNoiseImageResource for noise generator {noiseid}")


class NetworkSetNetworkGenFeatVisResource:

    def on_put(self, req, resp, networkid : int):
        network = networks[networkid]
        network.feature_visualization_mode = FeatureVisualizationMode.Generating
        print(f"NetworkSetNetworkGenFeatVisResource for network {networkid}")


class NetworkSetNetworkLoadFeatVisResource:

    def on_put(self, req, resp, networkid : int):
        network = networks[networkid]
        network.feature_visualization_mode = FeatureVisualizationMode.Loading
        print(f"NetworkSetNetworkLoadFeatVisResource for network {networkid}")


class NetworkSetNetworkDeleteFeatVisResource:

    def on_put(self, req, resp, networkid : int):
        network = networks[networkid]
        network.delete_feat_vis_cache()
        print(f"NetworkSetNetworkDeleteFeatVisResource for network {networkid}")


class NetworkExportLayerResource:

    def on_put(self, req, resp, networkid : int, layerid : int):
        jsonfile = json.load(req.stream)
        activation_image = ActivationImage(
            network_ID = jsonfile["networkID"],
            dataset_ID = jsonfile["datasetID"],
            image_ID = jsonfile["imageID"],
            layer_ID = jsonfile["layerID"],
            channel_ID = jsonfile["channelID"],
            noise_generator_ID = jsonfile["noiseGeneratorID"],
            mode = ActivationImage.Mode(jsonfile["mode"]))
        
        network = networks[networkid]
        network.export(activation_image)
        print(f"send NetworkPrepareForInputResource for network {networkid}")
        

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