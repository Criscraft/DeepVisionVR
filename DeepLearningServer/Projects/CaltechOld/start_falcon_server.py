import Scripts.DLWebServer as DLWebServer
import Scripts.Falconapp as falconapp
import run_server_caltech as run_server_caltech

# select the Python module, which defines what networks, datasets ect. should be loaded 
DLWebServer.select_project_server(run_server_caltech)
application = falconapp.start_server(DLWebServer)