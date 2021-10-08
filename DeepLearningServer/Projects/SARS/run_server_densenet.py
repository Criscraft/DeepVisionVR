import torch
import numpy as np
from CovidDenseNet import CovidDenseNet
from TransformTestGray import TransformTestGray
from TransformToTensor import TransformToTensor
from DatasetClassesFromFoldersNoiseShuffledLabels import DatasetClassesFromFoldersNoiseShuffledLabels
from Scripts.DLData import DLData
from Scripts.DLNetwork import DLNetwork
from Scripts.NoiseGenerator import NoiseGenerator


N_IMAGES = 16
N_CLASSES = 2
SEED = 42
DATAPATH = '/DeepLearningServer/Datasets/SARS-CoV-2-CT-dataset'
IMAGE_SHAPE = (180, 250)
NORM_MEAN = [0.485, 0.456, 0.406]
NORM_STD = [0.229, 0.224, 0.225]

use_cuda = torch.cuda.is_available()
device = torch.device("cuda") if use_cuda else torch.device("cpu")


def get_dl_networks():

    network_list = []

    model = CovidDenseNet(
        variant='densenet121',
        n_classes=N_CLASSES, 
        pretrained=False,
        blocks=[6, 10, 2, 1],
        statedict='coviddensenet_scratch.pt')
    dl_network = DLNetwork(model, device, NORM_MEAN, NORM_STD, IMAGE_SHAPE, 1)
    for param in model.embedded_model.parameters():
        param.requires_grad = False
    network_list.append(dl_network)

    model = CovidDenseNet(
        variant='densenet121',
        n_classes=N_CLASSES, 
        pretrained=False,
        blocks=[6, 10, 2, 1],
        statedict='coviddensenet_orig_and_noise_shuffle.pt')
    dl_network = DLNetwork(model, device, NORM_MEAN, NORM_STD, IMAGE_SHAPE, 2)
    for param in model.embedded_model.parameters():
        param.requires_grad = False
    network_list.append(dl_network)

    model = CovidDenseNet(
        variant='densenet121',
        n_classes=N_CLASSES, 
        pretrained=False,
        blocks=[6, 10, 2, 1],
        statedict='coviddensenet_shuffle.pt')
    dl_network = DLNetwork(model, device, NORM_MEAN, NORM_STD, IMAGE_SHAPE, 3)
    for param in model.embedded_model.parameters():
        param.requires_grad = False
    network_list.append(dl_network)

    return network_list


def get_datasets():

    transform_test = TransformTestGray(IMAGE_SHAPE, NORM_MEAN, NORM_STD)
    transform_to_tensor = TransformToTensor()

    dataset = DatasetClassesFromFoldersNoiseShuffledLabels(
        transform='transform_test', 
        datapath=DATAPATH, 
        copy_data_to='',
        b_train_fold = False,
        p_train = 0.8,
        custom_seed = 42)
    dataset.prepare({'transform_test' : transform_test})

    dataset_to_tensor = DatasetClassesFromFoldersNoiseShuffledLabels(
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
    noise_generator = NoiseGenerator(device, IMAGE_SHAPE, grayscale=True)
    return [noise_generator]