docker run --gpus all --name dl_linse_interactive --rm -v $(pwd):/app  -v /mnt:/mnt --shm-size 256M -it dl_cuda_deep_vision_vr /bin/bash