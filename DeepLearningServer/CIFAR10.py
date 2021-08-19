import torch.utils.data as data
from torchvision.datasets import CIFAR10 as CIFAR_ORIG
from sklearn.model_selection import (StratifiedShuffleSplit)


class CIFAR10(data.Dataset):

    def __init__(self,
        b_train=True,
        transform='',
        root='/data/tmp_data',
        download=False,
        tags={}):
        super().__init__()

        self.b_train = b_train
        self.transform_name = transform
        self.root = root
        self.download = download
        self.tags = tags
        self.class_names = (
            'airplane',
            'automobi',
            'bird',
            'cat',
            'deer',
            'dog',
            'frog',
            'horse',
            'ship',
            'truck',
        )


    def prepare(self, shared_modules):
        transform_fn = None
        if self.transform_name:
            transform_fn = shared_modules[self.transform_name]
        self.cifar_10 = CIFAR_ORIG(root=self.root, train=self.b_train, download=self.download, transform=transform_fn)
        
        
    def __len__(self):
        return len(self.cifar_10)


    def __getitem__(self, idx):
        item = self.cifar_10[idx]
        sample = {'data' : item[0], 'label' : item[1], 'id' : idx, 'tags' :  dict(self.tags)}
        return sample


    def get_balanced_shuffle_split_indices(self, n_or_p_train, b_train, seed):
        skf = StratifiedShuffleSplit(n_splits=1, train_size=n_or_p_train, random_state=seed)
        train_idx, test_idx = next(iter(skf.split(self.cifar_10.targets, self.cifar_10.targets)))
        if b_train:
            indices = train_idx
        else:
            indices = test_idx

        return indices

    
    