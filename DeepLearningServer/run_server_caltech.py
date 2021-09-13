import numpy as np
from ResNet_0_9 import ResNet_0_9
from TransformTest import TransformTest
from DLServer import DLServer
from DLPerformer import DLPerformer
from TransformToTensor import TransformToTensor
from DatasetClassesFromFolders import DatasetClassesFromFolders

def get_dl_performer():
    N_IMAGES = 16
    N_CLASSES = 102
    #DATAPATH = '/mnt/e/Dokumente/DeepVisVR/deeplearning/Caltech/data'
    DATAPATH = '/nfshome/linse/NO_INB_BACKUP/Data/Caltech'
    IMAGE_SHAPE = (150, 175)
    NORM_MEAN = [0.5487017, 0.5312975, 0.50504637]
    NORM_STD = [0.1878664, 0.18194826, 0.19830684]

    transform_test = TransformTest(IMAGE_SHAPE, NORM_MEAN, NORM_STD)
    transform_to_tensor = TransformToTensor()
    dataset = DatasetClassesFromFolders(transform='transform_test', datapath=DATAPATH, copy_data_to='')
    dataset.prepare({'transform_test' : transform_test})
    dataset_to_tensor = DatasetClassesFromFolders(transform='transform_to_tensor', datapath=DATAPATH, copy_data_to='')
    dataset_to_tensor.prepare({'transform_to_tensor' : transform_to_tensor})
    np.random.seed(42)
    data_indices = np.random.randint(len(dataset), size=N_IMAGES)
    model = ResNet_0_9(variant='resnet018', n_classes=N_CLASSES, statedict='resnet018_finetuning.pt')

    return DLPerformer(dataset, dataset_to_tensor, data_indices, N_CLASSES, model)