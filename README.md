# IIS Web Farm Monitor
This Service Fabric application monitors Services and pushes their HTTP and HTTPS service endpoints to IIS Application Request Routing server farms.
Each endpoint, across the cluster, is registered. Ports are updated.

A "monitor" is activated per watched service. This monitor contains some configuration:

```
{
  "Endpoints": {
    "ServiceEndpoint": {
      "ServerName": "",
      "ServerFarmName": "",
      "Interval": "00:00:15"
    }
  }
}
```

`ServerName` specifies the resolvable host name of the IIS machine to push updates to.
`ServerFarmName` specifies the ARR Server Farm name to update
`Interval` specifies the frequency at which to update the server farm.

Configuration can be retrieved or updated by sending a GET or PUT to the WebService endpoint at `/monitors/{ServiceName}`.

Additionally, auto configuration can be enabled by attaching Service Fabric Properties to services. These can be configured with `sfctl`

```
sfctl property put --name-id Application/Service --property-name IisWebFarmMonitor:Endpoints.{ServiceEndpoint}.ServerName --value '{ "Kind":"String", "Data":"IISHOSTNAME"  }'
sfctl property put --name-id Application/Service --property-name IisWebFarmMonitor:Endpoints.{ServiceEndpoint}.ServerFarmName --value '{ "Kind":"String", "Data":"ServerFarmName"  }'
sfctl property put --name-id Application/Service --property-name IisWebFarmMonitor:Endpoints.{ServiceEndpoint}.Interval --value '{ "Kind":"String", "Data":"00:00:15"  }'
```

