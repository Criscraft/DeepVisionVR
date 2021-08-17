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
- [ ] press button to increase the size of the Holoimage
- [x] Scrollen mit scroll view nach https://gyanendushekhar.com/2019/08/11/scroll-view-dynamic-content-size-unity-tutorial/
- [x] More flexible arrangement of layers
- [ ] Grabable Layers, Handle? No Handle? Special tool?
- [x] Histograms to show weight distribution of layer
- [x] Histograms to show activation distribution of layer
- [x] box and axis negative z position
- [x] color of boxes
- [x] histogram overflow
- [ ] tick labels smaller, or choose other format
- [x] activation histogram wrong axis at startup
- [x] do not update weight histogram when the input changes - only when network changes
- [ ] button to enable or disable histograms


Steps to enable/disable VR:
- Windows input debugger set lock input to game
- Steamvr input manager dvanced settings auto enable vr
- open xr settings initialize vr on startup