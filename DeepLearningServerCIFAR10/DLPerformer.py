import torch 
import numpy as np
from typing import Tuple
from ResNet_0_9_CIFAR import ResNet_0_9_CIFAR
from TransformTest import TransformTest
from TransformToTensor import TransformToTensor
from CIFAR10 import CIFAR10

N_IMAGES = 16
#DATAPATH = '/mnt/e/Dokumente/DeepVisVR/deeplearning/CIFAR10/data'
DATAPATH = '/data/tmp_data'
NCLASSES = 10

STATEDICT = 'baseline.pt'
#STATEDICT = 'labels_shuffled.pt'
#STATEDICT = 'labels_shuffled_finetuned_on_original.pt'
#STATEDICT = ''

class DLPerformer(object):
    
    def __init__(self, no_cuda = False):
        super().__init__()
        self.no_cuda = no_cuda
        global N_IMAGES
        global DATAPATH
        global STATEDICT
        global NCLASSES

        transform_test = TransformTest(
            norm_mean=[x / 255.0 for x in [125.3, 123.0, 113.9]], 
            norm_std=[x / 255.0 for x in [63.0, 62.1, 66.7]])
        transform_to_tensor = TransformToTensor()
        self.dataset = CIFAR10(b_train=False, transform='transform_test', root=DATAPATH, download=True, tags={})
        self.dataset.prepare({'transform_test' : transform_test})
        self.dataset_to_tensor = CIFAR10(b_train=False, transform='transform_to_tensor', root=DATAPATH, download=True, tags={})
        self.dataset_to_tensor.prepare({'transform_to_tensor' : transform_to_tensor})
        #self.data_indices = np.random.randint(len(self.dataset), size=N_IMAGES)
        self.data_indices = [8, 27, 563, 34, 166, 3, 67, 247, 74, 72, 89, 43, 82, 169, 317, 375]

        use_cuda = not no_cuda and torch.cuda.is_available()
        cuda_args = {'num_workers': 0, 'pin_memory': True} if use_cuda else {}
        self.device = torch.device("cuda") if use_cuda else torch.device("cpu")
        
        self.model = ResNet_0_9_CIFAR(variant='resnet018', n_classes=NCLASSES, statedict=STATEDICT)
        self.model = self.model.to(self.device)
        self.model.eval()

        self.features = None
        self.active_data_idx = None

    
    def get_data_overview(self):
        max_images = N_IMAGES
        return {'len' : min(max_images, len(self.dataset)), 'n_classes' : NCLASSES, 'type' : 'rgb', 'class_names' : self.dataset.class_names}

    
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