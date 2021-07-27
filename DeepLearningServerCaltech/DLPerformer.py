import torch 
import numpy as np
from typing import Tuple
from ResNet_0_9 import ResNet_0_9
from TransformTest import TransformTest
from TransformToTensor import TransformToTensor
from DatasetClassesFromFolders import DatasetClassesFromFolders

N_IMAGES = 16
#DATAPATH = '/mnt/e/Dokumente/DeepVisVR/deeplearning/Caltech/data'
DATAPATH = '/nfshome/linse/NO_INB_BACKUP/Data/Caltech'

class DLPerformer(object):
    
    def __init__(self, no_cuda = False):
        super().__init__()
        self.no_cuda = no_cuda

        transform_test = TransformTest(img_shape=(150, 175), 
            norm_mean=[0.5487017, 0.5312975, 0.50504637], 
            norm_std=[0.1878664, 0.18194826, 0.19830684])
        transform_to_tensor = TransformToTensor()
        self.dataset = DatasetClassesFromFolders(transform='transform_test', datapath=DATAPATH, copy_data_to='')
        self.dataset.prepare({'transform_test' : transform_test})
        self.dataset_to_tensor = DatasetClassesFromFolders(transform='transform_to_tensor', datapath=DATAPATH, copy_data_to='')
        self.dataset_to_tensor.prepare({'transform_to_tensor' : transform_to_tensor})
        global N_IMAGES
        self.data_indices = np.random.randint(len(self.dataset), size=N_IMAGES)

        use_cuda = not no_cuda and torch.cuda.is_available()
        cuda_args = {'num_workers': 0, 'pin_memory': True} if use_cuda else {}
        self.device = torch.device("cuda") if use_cuda else torch.device("cpu")
        
        self.model = ResNet_0_9(variant='resnet018', n_classes=102, statedict='resnet018_finetuning.pt')
        self.model = self.model.to(self.device)
        self.model.eval()

        self.features = None
        self.active_data_idx = None

    
    def get_data_overview(self):
        max_images = N_IMAGES
        return {'len' : min(max_images, len(self.dataset)), 'n_classes' : 102, 'type' : 'rgb', 'class_names' : self.dataset.class_names}

    
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
                features.append({'pos' : item['pos'], 'layer_name' : item['layer_name'], 'module_name' : item['module_name'], 'data_type' : data_type, 'size' : size, 'activation' : item.get('activation'), 'precursors' : item['precursors']})
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


    def pos_to_key(self, pos):
        return f"{pos[0]:02},{pos[1]:02}"


    def key_to_pos(self, key):
        key = key.split(",")
        return (int(key[0]), int(key[1]))