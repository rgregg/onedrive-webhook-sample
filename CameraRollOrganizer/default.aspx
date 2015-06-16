<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="default.aspx.cs" Inherits="CameraRollOrganizer._default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>OneDrive Camera Roll Organizer (sample)</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <h1>OneDrive Camera Roll Organizer</h1>

        <p>Sign in to your OneDrive to enable automatic camera roll organization. This sample application
        uses Webhooks and OneDrive API.</p>

        <p><asp:HyperLink runat="server" ID="linkToSignIn">Sign in to OneDrive</asp:HyperLink></p>
    </div>
    </form>
</body>
</html>
