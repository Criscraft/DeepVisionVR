import Scripts.DLWebServer as DLWebServer
import Scripts.Falconapp as falconapp
import run_server_resnet as run_server

# select the Python module, which defines what networks, datasets ect. should be loaded 
DLWebServer.select_project_server(run_server)
application = falconapp.start_server(DLWebServer)