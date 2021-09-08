from enum import Enum, auto

class ActivationImage(object):
    
    class Mode(Enum):
        Unknown = auto()
        DatasetImage = auto()
        Activation = auto()
        FeatureVisualization = auto()
        NoiseImage = auto()
        
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
    