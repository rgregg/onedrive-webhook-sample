# onedrive-webhook-sample

This example project demonestrates how to use webhooks from the OneDrive service to 
respond to changes in near real time on the serivce. In this case, the sample registers
for notifications on the camera roll folder. When a notification is received, the sample
scans for changes using view.delta, and then acts on any newly changed file items.

This sample is split into three projects:

* PhotoOrganizerWeb - A web API 2.0 application that runs the front-end web service
  for configuring how the app works and receiving webhook notifications.
* PhotoOrganizerWebJob - An Azure webjob service that runs in the background to process
  received webhook notifications, communicate with OneDrive, and make modifications to
  files as necessary.
* PhotoOrganizerShared - A shared set of code for managing accounts and notifications
  between the web and webjob projects.

This project is available for a real time demo here: https://camerarollorganizer.azurewebsites.net/.

