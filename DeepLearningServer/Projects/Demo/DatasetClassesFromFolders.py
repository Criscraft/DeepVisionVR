import torch.utils.data as data
import os
from shutil import copytree
import numpy as np
from PIL import Image
from multiprocessing import Manager
from collections import OrderedDict
from sklearn.model_selection import (StratifiedKFold,
                                    KFold,
                                    StratifiedShuffleSplit)


EXTENSIONS = set(['jpg', 'png'])


class DatasetClassesFromFolders(object):

    def __init__(self,
        datapath='',
        transform='',
        copy_data_to='/data/tmp_data',
        convert_to_rbg_image=True,
        tags={}):
        super().__init__()

        self.datapath = datapath
        self.transform_name = transform
        self.copy_data_to = copy_data_to
        self.convert_to_rbg_image = convert_to_rbg_image
        self.tags = tags
        
    def prepare(self, shared_modules):
        #load data to /tmp if not already there
        if self.copy_data_to:
            datapath_local = os.path.join(self.copy_data_to, self.datapath.split('/')[-1])
            if not os.path.isdir(datapath_local):
                copytree(self.datapath, datapath_local)
            self.datapath = datapath_local

        self.transform = None
        if self.transform_name:
            self.transform = shared_modules[self.transform_name]
        
        classfolder_paths = [os.path.join(self.datapath, name) for name in sorted(os.listdir(self.datapath)) if os.path.isdir(os.path.join(self.datapath, name))]
        self.n_classes = len(classfolder_paths)
        self.class_names = np.array([item.split('/')[-1] for item in classfolder_paths])

        self.image_paths = []
        self.image_labels = []
        self.label_to_indices = OrderedDict() #we need this for e.g. balanced sampling
        image_counter = 0
        for class_id, classfolder_path in enumerate(classfolder_paths):
            image_paths_tmp = [os.path.join(classfolder_path, name) for name in sorted(os.listdir(classfolder_path)) if name.split('.')[-1] in EXTENSIONS]
            self.image_paths.extend(image_paths_tmp)
            self.image_labels.extend([class_id for item in image_paths_tmp])
            self.label_to_indices[class_id] = np.arange(image_counter, image_counter+len(image_paths_tmp))
            image_counter += len(image_paths_tmp)
        
        manager = Manager() # use manager to improve the shared memory between workers which load data. Avoids the effect of ever increasing memory usage. See https://github.com/pytorch/pytorch/issues/13246#issuecomment-445446603
        self.tags = manager.dict(self.tags)
        self.image_paths = manager.list(self.image_paths)
        self.image_labels = manager.list(self.image_labels)
        self.class_counts = manager.list([len(item) for item in self.label_to_indices.values()])

    def __len__(self):
        return len(self.image_labels)


    def __getitem__(self, idx):
        imgname, label = self.image_paths[idx], self.image_labels[idx]
        out_image = Image.open(imgname)
        if self.convert_to_rbg_image:
            out_image = out_image.convert('RGB')
        if self.transform is not None:
            out_image = self.transform(out_image)
        sample = {'data' : out_image, 'label' : label, 'id' : idx, 'tags' : dict(self.tags), 'path' : imgname}
        return sample


    def get_stratified_k_fold_indices(self, fold, n_folds, b_train, seed):
        skf = StratifiedKFold(n_splits=n_folds, shuffle=True, random_state=np.random.RandomState(seed))
        train_idx, test_idx = list(skf.split(self.image_paths, self.image_labels))[fold]
        if b_train:
            return train_idx
        else:
            return test_idx


    def get_k_fold_indices(self, fold, n_folds, b_train, b_shuffle_before_splitting, seed):
        skf = KFold(n_splits=n_folds, shuffle=b_shuffle_before_splitting, random_state=np.random.RandomState(seed))
        train_idx, test_idx = list(skf.split(self.image_paths, self.image_labels))[fold]
        if b_train:
            return train_idx
        else:
            return test_idx


    def get_balanced_shuffle_split_indices(self, n_or_p_train, b_train, seed):
        skf = StratifiedShuffleSplit(n_splits=1, train_size=n_or_p_train, random_state=seed)
        train_idx, test_idx = next(iter(skf.split(self.image_paths, self.image_labels)))
        if b_train:
            indices = train_idx
        else:
            indices = test_idx

        return indices


    def get_class_weights(self):
        #if one class has double the frequency then it has half the weight than the other class. (weights*class_count).sum is always the total numner of samples.
        class_counts = np.array(self.class_counts)
        weights = class_counts.sum() / class_counts
        weights = weights * (class_counts.sum() / (weights*class_counts).sum())
        return weights