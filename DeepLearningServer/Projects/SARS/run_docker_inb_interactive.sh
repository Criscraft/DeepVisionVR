docker run --gpus all --name dl_linse_interactive --rm -v $(pwd):/app -v ../../:/DeepLearningServer -e PYTHONPATH=/DeepLearningServer/Scripts --shm-size 256M -p 5570:5570 -it dl_cuda_deep_vision_vr /bin/bash