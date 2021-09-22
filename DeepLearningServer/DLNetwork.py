import torch
import torch.nn.functional as F 
import numpy as np
from FeatureVisualizerRobust import FeatureVisualizer
from ActivationImage import ActivationImage


class DLNetwork(object):
    
    def __init__(self, model, device, norm_mean, norm_std, input_size):
        super().__init__()
        
        self.device = device
        self.model = model.to(self.device)
        for param in self.model.parameters():
            param.requires_grad = False
        self.model.eval()

        self.feature_visualizer = FeatureVisualizer(norm_mean=norm_mean, norm_std=norm_std, target_size=input_size)
        self.features = None
        self.active_data_item = ActivationImage()
        self.active_noise_image = None
        self.feature_visualizations = {}

    
    def prepare_for_input(self, activation_image : ActivationImage):
        with torch.no_grad():
            self.active_data_item = activation_image
            image = self.active_data_item.data.to(self.device)
            image = image.unsqueeze(0)
            out_dict = self.model.forward_features({'data' : image})
            features = []
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
            raise ValueError("You have to prepare the input first")
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


    def get_feature_visualization(self, layer_id):
        if self.features is None:
            raise ValueError("You have to prepare the input first")
            
        if layer_id == 0:
            return np.zeros((self.features[layer_id]["size"][1], 3, 1, 1))
        
        module = self.features[layer_id]["module"]
        n_channels = self.features[layer_id]["size"][1]
        
        created_images, _ = self.feature_visualizer.visualize(self.model, module, self.device, self.active_data_item.data, n_channels)
        visual_images = self.feature_visualizer.export_transformation(created_images)
        
        self.feature_visualizations[layer_id] = created_images
        return visual_images


    def get_classification_result(self):
        out = self.get_activation(len(self.features) - 1)
        out = F.softmax(out, 1) * 100
        out = out[0].cpu().numpy()
        indices_tmp = np.argsort(-out)
        indices_tmp = indices_tmp[:10]
        return out[indices_tmp], indices_tmp