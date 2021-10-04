import torch
import torch.utils.data as data
from DatasetClassesFromFolders import DatasetClassesFromFolders

class DatasetClassesFromFoldersNoiseShuffledLabels(data.Dataset):

    def __init__(self,
        datapath='',
        transform='',
        copy_data_to='/data/tmp_data',
        convert_to_rbg_image=True,
        tags={},
        b_train_fold=True,
        p_train=0.8,
        custom_seed=42,
        rel_amount_original_images = 1.,
        rel_amount_noise_images = 0.,
        random_labels_for_noise_images = False,
        blend_alpha=0.):
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
        self.rel_amount_original_images = rel_amount_original_images
        self.rel_amount_noise_images = rel_amount_noise_images
        self.random_labels_for_noise_images = random_labels_for_noise_images
        self.add_gaussian_noise = AddGaussianNoise(blend_alpha)
        

    def prepare(self, shared_modules):
        self.embedded_dataset.prepare(shared_modules)
        self.class_names = self.embedded_dataset.class_names
        self.used_indices = self.embedded_dataset.get_balanced_shuffle_split_indices(n_or_p_train=self.p_train, b_train=self.b_train_fold, seed=self.custom_seed)
        
        
    def __len__(self):
        return int(( self.rel_amount_original_images + self.rel_amount_noise_images ) * len(self.used_indices))


    def __getitem__(self, idx):
        item = self.embedded_dataset[self.used_indices[idx % len(self.used_indices)]]
        img = item["data"]
        label = item["label"]
        if idx >= self.rel_amount_original_images * len(self.used_indices):
            img = self.add_gaussian_noise(img, idx)
            if self.random_labels_for_noise_images:
                random_state = torch.random.manual_seed(idx)
                label = torch.randint(0, len(self.class_names), (1,), generator=random_state).item()
        sample = {'data' : img, 'label' : label, 'id' : idx, 'tags' :  item["tags"]}
        return sample
    
class AddGaussianNoise(object):

    def __init__(self, blend_alpha):
        self.blend_alpha = blend_alpha

    def __call__(self, img, seed):
        random_state = torch.random.manual_seed(seed)
        noise_image = torch.randn(img.shape, generator=random_state)
        img = img + self.blend_alpha * noise_image
        return img
