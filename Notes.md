## Useful Links
https://math.stackexchange.com/questions/126062/finding-a-point-on-archimedean-spiral-by-its-path-length
https://downloads.imagej.net/fiji/snapshots/arc_length.pdf


## TODO

- [x] Edges and network on the floor
- [x] Text describtion to the edges
- [x] Bug when one puts two images into the holder
- [x] Bug RGB of input image
- [x] Bezier first or last segment missing
- [x] Show label above portable image frame
- [x] Show label at network ouput
- [x] Dynamic Spacing of branching
- [x] Dynamic Spacing of layers depending on size
- [x] UI Screen to pick image from dataset, a portable image frame is spawned
- [x] Grap and throw away close to the VR implementation
- [x] Farbkodierung, um negative und positive Werte zu unterscheiden, color of frame indicates correct or wrong classification
- [x] Klassifikationsergebniss mit prozenten anzeigen, auch nahe am input
- [ ] scale size of the network
- [ ] add interaction to change network layout
- [x] DATAPATH in DLPerformer.py
- [x] press right mouse button to increase the size of the Holoimage
- [x] press c button to save the texture to disk
- [x] Scrollen mit scroll view nach https://gyanendushekhar.com/2019/08/11/scroll-view-dynamic-content-size-unity-tutorial/
- [x] More flexible arrangement of layers
- [x] Histograms to show weight distribution of layer
- [x] Histograms to show activation distribution of layer
- [x] box and axis negative z position
- [x] color of boxes
- [x] histogram overflow
- [x] tick labels smaller, or choose other format
- [x] activation histogram wrong axis at startup
- [x] do not update weight histogram when the input changes - only when network changes
- [ ] button to enable or disable histograms
- [x] fix loading the image input frame
- [x] all feature maps are imagegetterbuttons
- [x] fix tools get stuck in ground when dropped
- [x] fix VR mode
- [ ] Teleportation Mode in VR
- [ ] VR UI for enabling disabling teleportation mode and free movement
- [x] no zoom if image selecter unequiped
- [x] batch size for image visualization
- [x] aspect ratio when zooming


## Notes
Steps to enable/disable VR:
- Windows input debugger set lock input to game
- Steamvr input manager dvanced settings auto enable vr
- open xr settings initialize vr on startup