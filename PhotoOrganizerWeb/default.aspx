<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Async="true" Inherits="PhotoOrganizerWeb._default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>OneDrive Camera Roll Organizer (sample)</title>
    <link href="styles.css" rel="stylesheet" />
    <link rel="stylesheet" href="https://appsforoffice.microsoft.com/fabric/1.0/fabric.min.css" />
    <link rel="stylesheet" href="https://appsforoffice.microsoft.com/fabric/1.0/fabric.components.min.css" />
    <script src="https://ajax.aspnetcdn.com/ajax/jQuery/jquery-2.1.4.min.js" type="text/javascript"></script>
</head>
<body>
    <form id="form1" runat="server">
    <div>

        <div class="ms-Grid">
            <div class="ms-Grid-row">
                <div class="ms-Grid-col ms-u-sm12">
                    <h1 class="ms-font-xxl ms-fontColor-themeDark">OneDrive Camera Roll Organizer Sample</h1>
                </div>
            </div>

            <div class="ms-Grid-row" id="panelNoCurrentUser" runat="server">
                <div class="ms-Grid-col ms-u-sm12 ms-u-lg8">
                    <p class="ms-font-m">Sign in to your OneDrive to get started. This sample application
                    uses Webhooks and OneDrive API to reclaim your camera roll folder by organizing your photos into folders by month and year.</p>

                    <asp:HyperLink runat="server" ID="linkToSignIn" CssClass="ms-Button ms-Button--primary"><span class="ms-Button-label">Sign in</span></asp:HyperLink>
                </div>
            </div>
        </div>


        <div class="ms-Grid-row" id="panelLoggedIn" runat="server">
            <div class="ms-Grid-col ms-u-sm12 ms-u-lg8">
                <h4 class="ms-font-l">This web robot keeps your photos organized for you!</h4>
                <p class="ms-font-m">When new files are added to your <a href="https://onedrive.live.com?id=wmphotos" target="_blank">Camera Roll folder</a> in 
                    OneDrive, our robot will automatically move them into a folder based on the date the photo was taken. Files that don't have a date taken won't be moved.</p>
            
                <h3 class="ms-font-xl ms-bgColor-themeDark ms-fontColor-white">Configuration</h3>
                <p class="ms-font-m">You can change the way camera roll organizer works on your account. These changes only apply
                    to items not already organized.</p>

                <div class="ms-TextField">
                    <label class="ms-Label">Destination folder name</label> 
                    <input class="ms-TextField-field" type="text" id="textBoxFolderFormatString" runat="server" />
                    <span class="ms-TextField-description">This is passed into String.Format(format, DateTimeOfFile), so you can use {0} or {0:format} to customize the folder names.</span>
                </div>

                <div class="ms-Toggle ms-font-m">
                    <span class="ms-Toggle-description">Keep my camera roll organized</span>
                    <input type="checkbox" id="checkBoxEnableAccount" class="ms-Toggle-input" runat="server" />
                    <label for="checkBoxEnableAccount" class="ms-Toggle-field"><span class="ms-Label ms-Label--off">Off</span> <span class="ms-Label ms-Label--on">On</span></label>
                </div>

                <p class="ms-font-m"><asp:Button runat="server" ID="buttonSaveChanges" Text="Save" OnClick="buttonSaveChanges_Click" CssClass="ms-Button"></asp:Button></p>
                <asp:Label runat="server" ID="labelErrors" ForeColor="Red" Text="" CssClass="ms-font-m"></asp:Label>

                <h3 class="ms-font-xl ms-bgColor-themeDark ms-fontColor-white">Statistics</h3>
                <p class="ms-font-m">Items in your camera roll: <span id="itemsInCameraRoll">Loading ...</span></p>
                <p class="ms-font-m">Size of your camera roll: <span id="sizeOfCameraRoll">Loading ...</span></p>
                <p class="ms-font-m">Camera roll last modified: <span id="lastModifiedCameraRoll">Loading ...</span></p>
                <p class="ms-font-m">Photos organized: <asp:Label runat="server" ID="labelPhotosOrganizedCount"></asp:Label></p>
                <p class="ms-font-m">Webhooks received: <asp:Label runat="server" ID="labelWebhooksReceived"></asp:Label></p>
                <p class="ms-font-m">Processing queue depth: <span id="processingQueueDepth">Loading...</span></p>

                <div class="ms-font-m">
                    <a href="/signout" class="ms-Button ms-Button--primary"><span class="ms-Button-label">Sign Out</span></a>
                    <a href="/api/action/activity" class="ms-Button ms-Button--primary"><span class="ms-Button-label">Activity Log</span></a>
                    <button class="ms-Button ms-Button--primary" onclick="queueTestWebhook(); return false;"><span class="ms-Button-label">Generate test webhook</span></button>
                </div>
            </div>
        </div>


    </div>
    </form>

    <script src="photoOrganizer.js" type="text/javascript"></script>
</body>
</html>
