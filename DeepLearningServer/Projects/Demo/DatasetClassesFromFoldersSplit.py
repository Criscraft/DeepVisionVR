import torch
import torch.utils.data as data
from DatasetClassesFromFolders import DatasetClassesFromFolders

class DatasetClassesFromFoldersSplit(data.Dataset):

    def __init__(self,
        datapath='',
        transform='',
        copy_data_to='',
        convert_to_rbg_image=True,
        tags={},
        b_train_fold=True,
        p_train=0.8,
        custom_seed=42):
        super().__init__()
        
        self.embedded_dataset = DatasetClassesFromFolders(
            datapath=datapath,
            transform=transform,
            copy_data_to=copy_data_to,
            convert_to_rbg_image=convert_to_rbg_image,
            tags=tags)
        
        self.b_train_fold = b_train_fold
        self.p_train = p_train
        self.custom_seed = custom_seed
        

    def prepare(self, shared_modules):
        self.embedded_dataset.prepare(shared_modules)
        self.class_names = self.embedded_dataset.class_names
        self.used_indices = self.embedded_dataset.get_balanced_shuffle_split_indices(n_or_p_train=self.p_train, b_train=self.b_train_fold, seed=self.custom_seed)
        
        
    def __len__(self):
        return len(self.used_indices)


    def __getitem__(self, idx):
        return self.embedded_dataset[self.used_indices[idx]]