from enum import Enum

class ActivationImage(object):
    
    class Mode(Enum):
        Unknown = 0
        DatasetImage = 1
        Activation = 2
        FeatureVisualization = 3
        NoiseImage = 4
        
    def __init__(self, 
        network_ID : int = -1,
        dataset_ID : int = -1,
        noise_generator_ID : int = -1,
        image_ID : int = -1,
        layer_ID : int = -1,
        channel_ID : int = -1,
        mode : Mode = Mode.Unknown,
        data = None):
        
        super().__init__()
        self.network_ID = network_ID
        self.dataset_ID = dataset_ID
        self.noise_generator_ID = noise_generator_ID
        self.image_ID = image_ID
        self.layer_ID = layer_ID
        self.channel_ID = channel_ID
        self.mode = mode
        self.data = data
    