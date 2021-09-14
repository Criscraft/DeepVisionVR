import torch
import torch.nn.functional as F 
import numpy as np
import base64
import cv2
from FeatureVisualizer import FeatureVisualizer
from ActivationImage import ActivationImage


class TransformToUint(object):

    def __init__(self):
        self.v_max = 0
        self.v_min = 0

    def __call__(self, data, fitting):
        if isinstance(data, torch.Tensor):
            data = data.cpu().numpy()
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
        data = data.astype("uint8")
        return data


class DLPerformer(object):
    
    def __init__(self, dataset, dataset_to_tensor, data_indices, n_classes, model, norm_mean=(0.,0.,0.), norm_std=(1.,1.,1.), no_cuda=False):
        super().__init__()
        self.dataset = dataset
        self.dataset_to_tensor = dataset_to_tensor
        self.data_indices = data_indices
        self.n_classes = n_classes
        self.norm_mean = norm_mean
        self.norm_std = norm_std
        self.no_cuda = no_cuda
        
        use_cuda = not no_cuda and torch.cuda.is_available()
        self.device = torch.device("cuda") if use_cuda else torch.device("cpu")
        
        self.model = model.to(self.device)
        for param in self.model.parameters():
            param.requires_grad = False
        self.model.eval()

        self.feature_visualizer = FeatureVisualizer(norm_mean=self.norm_mean, norm_std=self.norm_std)
        self.features = None
        self.active_data_item = ActivationImage()
        self.active_noise_image = None
        self.feature_visualizations = {}

        self.tensor_to_uint_transform = TransformToUint()
        

    def tensor_to_string(self, tensor):
        image = tensor.cpu().numpy()
        image = image * 255
        image = image.astype("uint8")
        image = image[np.array([2,1,0])] # for sorting color channels
        image = image.transpose([1,2,0]) # put channel dimension to last
        image_enc = self.encode_image(image)
        return image_enc


    def encode_image(self, data):
        _, buffer = cv2.imencode('.png', data)
        return base64.b64encode(buffer).decode('utf-8')


    def generate_noise_image(self):
        self.active_noise_image = self.feature_visualizer.generate_noise_image(self.device)
        return self.active_noise_image

    
    def get_data_overview(self):
        return {'len' : len(self.data_indices), 'class_names' : self.dataset.class_names}

    
    def get_data_item(self, idx: int):
        item = self.dataset_to_tensor[self.data_indices[idx]]
        return item['data'], item['label']


    def prepare_for_input(self, activation_image : ActivationImage):
        with torch.no_grad():
            if activation_image.mode == ActivationImage.Mode.DatasetImage and activation_image.image_ID >= 0:
                image = self.dataset[self.data_indices[activation_image.image_ID]]['data']
            elif activation_image.mode == ActivationImage.Mode.FeatureVisualization:
                image = self.feature_visualizations[activation_image.layer_ID][activation_image.channel_ID]
            elif activation_image.mode == ActivationImage.Mode.NoiseImage:
                if self.active_noise_image is None:
                    self.generate_noise_image()
                image = self.active_noise_image
            else:
                image = torch.zeros(self.dataset[0]['data'].shape)
            activation_image.data = image
            self.active_data_item = activation_image
            
            image = image.to(self.device)
            image = image.unsqueeze(0)
            out_dict = self.model.forward_features({'data' : image})
            features = []
            #raw_image = self.dataset_to_tensor[self.data_indices[idx]]['data']
            #raw_image = raw_image.unsqueeze(0)
            #features.append({'pos' : (0,0), 'layer_name' : 'RGB input', 'module_name' : '', 'data_type' : '2D_feature_map', 'size' : raw_image.shape, 'activation' : raw_image, 'precursors' : []})
            for item in out_dict['activations']:
                if 'activation' in item:
                    if len(item['activation'].shape) == 4:
                        data_type = '2D_feature_map'
                    elif len(item['activation'].shape) == 2:
                        data_type = '1D_vector'
                    size = item['activation'].shape
                else:
                    data_type = 'None'
                    size = [0]
                features.append({'pos' : item['pos'], 'layer_name' : item['layer_name'], 'tracked_module' : item['tracked_module'], 'module' : item['module'], 'data_type' : data_type, 'size' : size, 'activation' : item.get('activation'), 'precursors' : item['precursors']})
            self.features = features



    def get_architecture(self):
        if self.features is None:
            activation_image = ActivationImage(mode = ActivationImage.Mode.DatasetImage)
            self.prepare_for_input(activation_image)
        layerdict_list = [{key:value[key] for key in ('pos', 'layer_name', 'data_type', 'size', 'precursors')} for value in self.features]
        return { 'architecture' : layerdict_list }


    def get_activation(self, layer_id):
        if self.features is None:
            raise ValueError("did not prepare for input yet")
        return self.features[layer_id]['activation']


    def get_weights(self, layer_id):
        module = self.features[layer_id]["tracked_module"]
        if module is not None and hasattr(module, "weight"):
            return module.weight
        else:
            return None


    def pos_to_key(self, pos):
        return f"{pos[0]:02},{pos[1]:02}"


    def key_to_pos(self, key):
        key = key.split(",")
        return (int(key[0]), int(key[1]))


    def get_feature_visualization(self, layer_id, debug=False):
        if self.features is None:
            self.prepare_for_input(0)
            
        if layer_id == 0:
            return np.zeros((self.features[layer_id]["size"][1], 3, 1, 1))
        
        module = self.features[layer_id]["module"]
        n_channels = self.features[layer_id]["size"][1]
        
        if not debug:
            export_images, created_images = self.feature_visualizer.visualize(self.model, module, self.device, n_channels, self.active_data_item.data)
        else:
            return self.feature_visualizer, self.model, module, self.device, n_channels, self.active_data_item.data
        
        self.feature_visualizations[layer_id] = created_images
        return export_images


    def get_classification_result(self):
        out = self.get_activation(len(self.features) - 1)
        out = F.softmax(out, 1) * 100
        out = out[0].cpu().numpy()
        indices_tmp = np.argsort(-out)
        indices_tmp = indices_tmp[:16]
        return out[indices_tmp], indices_tmp