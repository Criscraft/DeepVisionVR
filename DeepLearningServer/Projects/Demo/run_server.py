import torch
import numpy as np
from CovidResNet import CovidResNet
from TransformTestRGB import TransformTestRGB
from TransformToTensor import TransformToTensor
from DatasetClassesFromFoldersSplit import DatasetClassesFromFoldersSplit
from Scripts.DLData import DLData
from Scripts.DLNetwork import DLNetwork
from Scripts.NoiseGenerator import NoiseGenerator


N_IMAGES = 16
N_CLASSES = 101
SEED = 42
DATAPATH = '/DeepLearningServer/Datasets/Caltech'
IMAGE_SHAPE = (150, 175)
NORM_MEAN = [0.5487017, 0.5312975, 0.50504637]
NORM_STD = [0.1878664, 0.18194826, 0.19830684]

use_cuda = torch.cuda.is_available()
device = torch.device("cuda") if use_cuda else torch.device("cpu")


def get_dl_networks():

    network_list = []
    
    
    model = CovidResNet(
        variant='resnet018',
        n_classes=N_CLASSES, 
        pretrained=False,
        blocks=[2,1,1,1],
        statedict='covidresnet_augmentation.pt')
    dl_network = DLNetwork(model, device, NORM_MEAN, NORM_STD, IMAGE_SHAPE, 0)
    for param in model.embedded_model.parameters():
        param.requires_grad = False
    network_list.append(dl_network)
    
    return network_list


def get_datasets():

    transform_test = TransformTestRGB(IMAGE_SHAPE, NORM_MEAN, NORM_STD)
    transform_to_tensor = TransformToTensor()

    dataset = DatasetClassesFromFoldersSplit(
        transform='transform_test', 
        datapath=DATAPATH, 
        copy_data_to='',
        b_train_fold = False,
        p_train = 0.8,
        custom_seed = 42)
    dataset.prepare({'transform_test' : transform_test})

    dataset_to_tensor = DatasetClassesFromFoldersSplit(
        transform='transform_to_tensor', 
        datapath=DATAPATH, 
        copy_data_to='',
        b_train_fold = False,
        p_train = 0.8,
        custom_seed = 42)
    dataset_to_tensor.prepare({'transform_to_tensor' : transform_to_tensor})

    np.random.seed(SEED)
    data_indices = np.random.randint(len(dataset), size=N_IMAGES)

    dldata = DLData(dataset, dataset_to_tensor, data_indices, dataset.class_names, N_CLASSES)
    
    return [dldata]


def get_noise_generators():
    noise_generator = NoiseGenerator(device, IMAGE_SHAPE, grayscale=False)
    return [noise_generator]