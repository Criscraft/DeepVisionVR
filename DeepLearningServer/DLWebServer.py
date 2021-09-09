import json
import time
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
        print("send NetworkActivationImageResource")


class NetworkPrepareForInputResource:

    def on_put(self, req, resp):
        jsonfile = json.load(req.stream)
        print(jsonfile)
        activation_image = ActivationImage(
            image_ID = jsonfile["imageID"],
            layer_ID = jsonfile["layerID"],
            channel_ID = jsonfile["channelID"],
            mode = ActivationImage.Mode(jsonfile["mode"]))
        print(activation_image.image_ID)
        print(activation_image.layer_ID)
        print(activation_image.channel_ID)
        print(activation_image.mode)
        dl_performer.prepare_for_input(activation_image)
        print("send NetworkPrepareForInputResource")


class DataImagesResource:

    def on_get(self, req, resp):
        out = dl_performer.get_data_overview()
        tensors, class_names = [], []
        for i in range(out["len"]):
            tensor, label = dl_performer.get_data_item(i)
            class_names.append(dl_performer.dataset.class_names[label])
            tensors.append(dl_performer.tensor_to_string(tensor))
        out['tensors'] = tensors
        out["class_names"] = class_names
        resp.text = json.dumps(out, indent=1, ensure_ascii=False)
        print("send DataImagesResource")


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