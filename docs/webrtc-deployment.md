# WebRTC deployment

Calls need a publicly reachable HTTPS application URL and a TURN relay. STUN alone cannot connect callers behind some corporate firewalls, carrier-grade NATs, or symmetric NATs.

## Azure Communication Services (managed TURN)

Azure Communication Services Network Traversal is the preferred setup for this application. Create an Azure Communication Services resource, then add its connection string to the production application's environment variables:

```text
AzureCommunication__ConnectionString=endpoint=https://your-resource.communication.azure.com/;accesskey=...
```

The application uses this value on the server to request a new short-lived relay configuration whenever a room opens. Only the temporary TURN URLs, username, and credential are sent to the browser; the Azure connection string never leaves the server.

For an Azure App Service, add the value under **Settings** > **Environment variables**, save, and restart the app. Do not add it to `appsettings.json` or commit it to source control.

## Other TURN providers

Use a TURN provider or run coturn on a public server. Enable UDP and TCP on port 3478, plus TLS on port 5349 where possible. Open the TURN relay port range configured by the service (coturn commonly uses UDP 49152-65535).

Set the values as production environment variables; do not commit TURN credentials:

```text
WebRtc__Turn__Urls__0=turn:turn.example.com:3478?transport=udp
WebRtc__Turn__Urls__1=turn:turn.example.com:3478?transport=tcp
WebRtc__Turn__Urls__2=turns:turn.example.com:5349?transport=tcp
WebRtc__Turn__Username=replace-with-turn-username
WebRtc__Turn__Credential=replace-with-turn-password
```

The room page sends these credentials only to authenticated participants so their browser can use the relay. Prefer short-lived TURN credentials from your provider. Once all three values are supplied, the warning in the browser console disappears and the relay is included in the `RTCPeerConnection` ICE configuration.

## Host the application

- Publish the application at a public `https://` URL with a trusted certificate. Browsers block camera and microphone access on ordinary HTTP pages.
- Configure any reverse proxy or CDN to allow WebSocket upgrades to `/sessionHub` and to forward `X-Forwarded-Proto` so the app recognizes HTTPS correctly.
- With more than one application instance, add a SignalR backplane and replace the in-memory session/waiting-room maps in `SessionHub`. They are process-local and are suitable only for a single app instance.

## Verify

Join the same session from two unrelated networks, such as home broadband and mobile data. In each browser's WebRTC internals, the selected candidate pair should show `relay` when direct connectivity is unavailable. The room console logs ICE gathering and relay errors with the affected TURN URL.
