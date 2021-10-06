import os
import shutil
import torch
import torch.nn.functional as F 
import numpy as np
from enum import Enum
import cv2
import Scripts.utils as utils
from Scripts.FeatureVisualizerRobust import FeatureVisualizer
from Scripts.ActivationImage import ActivationImage


class FeatureVisualizationMode(Enum):
    Generating = 0
    Loading = 1


class DLNetwork(object):
    
    def __init__(self, model, device, norm_mean, norm_std, input_size, network_id):
        super().__init__()
        
        self.device = device
        self.network_id = network_id
        self.model = model.to(self.device)
        for param in self.model.parameters():
            param.requires_grad = False
        self.model.eval()

        self.feature_visualizer = FeatureVisualizer(norm_mean=norm_mean, norm_std=norm_std, target_size=input_size)
        self.features = None
        self.active_data_item = ActivationImage()
        self.active_noise_image = None

        self.cache_path = f"cache/network{self.network_id}"
        if not os.path.exists(self.cache_path): os.makedirs(self.cache_path)

        self.feature_visualization_path = os.path.join(self.cache_path, 'FeatureVisualizations')
        if not os.path.exists(self.feature_visualization_path): os.makedirs(self.feature_visualization_path)
        self.feature_visualization_mode = FeatureVisualizationMode.Loading

        self.export_path = os.path.join(self.cache_path, 'Export')
        if not os.path.exists(self.export_path): os.makedirs(self.export_path)
        
    
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
        
        created_images = None
        is_loaded = False
        if self.feature_visualization_mode == FeatureVisualizationMode.Loading:
            created_images = self.try_load_feature_visualization(layer_id)
            is_loaded = True
        if created_images is None:
            is_loaded = False
            module = self.features[layer_id]["module"]
            n_channels = self.features[layer_id]["size"][1]
            created_images, _ = self.feature_visualizer.visualize(self.model, module, self.device, self.active_data_item.data, n_channels)
        
        visual_images = self.feature_visualizer.export_transformation(created_images)

        if not is_loaded: 
            self.save_feature_visualization(layer_id, created_images)
        
        return visual_images


    def get_classification_result(self):
        out = self.get_activation(len(self.features) - 1)
        out = F.softmax(out, 1) * 100
        out = out[0].cpu().numpy()
        indices_tmp = np.argsort(-out)
        indices_tmp = indices_tmp[:10]
        return out[indices_tmp], indices_tmp


    def delete_feat_vis_cache(self):
        shutil.rmtree(self.feature_visualization_path)
        os.makedirs(self.feature_visualization_path)

    
    def try_load_feature_visualization(self, layerid):
        path = os.path.join(self.feature_visualization_path, f"layer_{layerid}.pt")
        if os.path.exists(path):
            created_images = torch.load(path, 'cpu')
        else:
            created_images = None
        return created_images


    def save_feature_visualization(self, layerid, created_images):
        path = os.path.join(self.feature_visualization_path, f"layer_{layerid}.pt")
        torch.save(created_images, path)

    
    def export(self, activation_image : ActivationImage):
        images = None
        if activation_image.mode == ActivationImage.Mode.FeatureVisualization:
            images = self.get_feature_visualization(activation_image.layer_ID)
            images = images[:,np.array([2, 1, 0])] # for sorting color channels
            images = images.transpose([0, 2, 3, 1]) # put channel dimension to last
        elif activation_image.mode == ActivationImage.Mode.Activation:
            tensor = self.get_activation(activation_image.layer_ID)
            # give first element of activations because we do not want to have the batch dimension
            tensor = tensor[0]
            tensor_to_uint_transform = utils.TransformToUint()
            images = tensor_to_uint_transform(tensor, True)
        
        path = os.path.join(self.export_path, f"layer_{activation_image.layer_ID}")
        if not os.path.exists(path): os.makedirs(path)
        for i, image in enumerate(images):
            cv2.imwrite(os.path.join(path, f"{i}.png"), image)


