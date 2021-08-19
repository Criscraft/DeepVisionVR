import torch
from FeatureVisualizer import FeatureVisualizer


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
        self.active_data_idx = None

    
    def get_data_overview(self):
        return {'len' : len(self.data_indices), 'n_classes' : self.n_classes, 'type' : 'rgb', 'class_names' : self.dataset.class_names}

    
    def get_data_item(self, idx: int):
        item = self.dataset_to_tensor[self.data_indices[idx]]
        return {k:item[k] for k in ('data','label')}


    def prepare_for_input(self, idx: int):
        with torch.no_grad():
            if idx >= 0:
                image = self.dataset[self.data_indices[idx]]['data']
            else:
                image = torch.zeros(self.dataset[0]['data'].shape)
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
            self.active_data_idx = idx



    def get_architecture(self):
        if self.features is None:
            self.prepare_for_input(-1)
        layerdict_list = [{key:value[key] for key in ('pos', 'layer_name', 'data_type', 'size', 'precursors')} for value in self.features]
        return layerdict_list


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
            self.prepare_for_input(0)
        
        module = self.features[layer_id]["module"]
        n_channels = self.features[layer_id]["size"][1]
        visualizations = self.feature_visualizer.visualize(self.model, module, self.device, n_channels)
        return visualizations