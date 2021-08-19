import numpy as np
from ResNet_0_9_CIFAR import ResNet_0_9_CIFAR
from TransformCIFARTest import TransformCIFARTest
from DLServer import DLServer
from DLPerformer import DLPerformer
from TransformToTensor import TransformToTensor
from CIFAR10 import CIFAR10

N_IMAGES = 16
N_CLASSES = 10
#DATAPATH = '/mnt/e/Dokumente/DeepVisVR/deeplearning/CIFAR10/data'
DATAPATH = '/data/tmp_data'
NORM_MEAN = [x / 255.0 for x in [125.3, 123.0, 113.9]]
NORM_STD = [x / 255.0 for x in [63.0, 62.1, 66.7]]
STATEDICT = 'ResNet_0_9_CIFAR_baseline.pt'
#STATEDICT = 'ResNet_0_9_CIFAR_labels_shuffled.pt'
#STATEDICT = 'ResNet_0_9_CIFAR_labels_shuffled_finetuned_on_original.pt'
#STATEDICT = ''

transform_test = TransformCIFARTest(NORM_MEAN, NORM_STD)
transform_to_tensor = TransformToTensor()

dataset = CIFAR10(b_train=False, transform='transform_test', root=DATAPATH, download=True, tags={})
dataset.prepare({'transform_test' : transform_test})
dataset_to_tensor = CIFAR10(b_train=False, transform='transform_to_tensor', root=DATAPATH, download=True, tags={})
dataset_to_tensor.prepare({'transform_to_tensor' : transform_to_tensor})
data_indices = np.random.randint(len(dataset), size=N_IMAGES)

model = ResNet_0_9_CIFAR(variant='resnet018', n_classes=N_CLASSES, statedict=STATEDICT)

dl_performer = DLPerformer(dataset, dataset_to_tensor, data_indices, N_CLASSES, model, norm_mean=NORM_MEAN, norm_std=NORM_STD)
dl_server = DLServer(dl_performer)
dl_server.run()