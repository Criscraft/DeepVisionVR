from enum import Enum, auto

class ActivationImage(object):
    
    class Mode(Enum):
        Unknown = 0
        DatasetImage = 1
        Activation = 2
        FeatureVisualization = 3
        NoiseImage = 4
        
    def __init__(self, 
        image_ID = -1,
        layer_ID = -1,
        channel_ID = -1,
        mode = Mode.Unknown,
        data = None):
        
        super().__init__()
        self.image_ID = image_ID
        self.layer_ID = layer_ID
        self.channel_ID = channel_ID
        self.mode = mode
        self.data = data
    