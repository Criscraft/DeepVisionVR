import torch
import numpy as np
from ResNet_0_9 import ResNet_0_9
from TransformTest import TransformTest
from TransformToTensor import TransformToTensor
from DatasetClassesFromFoldersNoiseShuffledLabels import DatasetClassesFromFoldersNoiseShuffledLabels
from Scripts.DLData import DLData
from Scripts.DLNetwork import DLNetwork
from Scripts.NoiseGenerator import NoiseGenerator


N_IMAGES = 16
N_CLASSES = 102
SEED = 42
DATAPATH = '/DeepLearningServer/Datasets/Caltech'
IMAGE_SHAPE = (245, 300)
NORM_MEAN = [0.5487017, 0.5312975, 0.50504637]
NORM_STD = [0.1878664, 0.18194826, 0.19830684]

use_cuda = torch.cuda.is_available()
device = torch.device("cuda") if use_cuda else torch.device("cpu")


def get_dl_networks():
    
    model = ResNet_0_9(variant='resnet018', n_classes=N_CLASSES, statedict='model_caltech_baseline.pt')
    dlnetwork_baseline = DLNetwork(model, device, NORM_MEAN, NORM_STD, IMAGE_SHAPE, 0)
    for param in model.embedded_model.parameters():
        param.requires_grad = False

    return [dlnetwork_baseline]


def get_datasets():

    transform_test = TransformTest(IMAGE_SHAPE, NORM_MEAN, NORM_STD)
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
    noise_generator = NoiseGenerator(device, IMAGE_SHAPE)
    return [noise_generator]