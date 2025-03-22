@description('The location used for all deployed resources')
param location string = resourceGroup().location

@description('Tags that will be applied to all resources')
param tags object = {}


param dbchatProExists bool
@secure()
param dbchatProDefinition object

@description('Id of the user or app to assign application roles')
param principalId string

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = uniqueString(subscription().id, resourceGroup().id, location)

// Monitor application with Azure Monitor
module monitoring 'br/public:avm/ptn/azd/monitoring:0.1.0' = {
  name: 'monitoring'
  params: {
    logAnalyticsName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
    applicationInsightsDashboardName: '${abbrs.portalDashboards}${resourceToken}'
    location: location
    tags: tags
  }
}

// Container registry
module containerRegistry 'br/public:avm/res/container-registry/registry:0.1.1' = {
  name: 'registry'
  params: {
    name: '${abbrs.containerRegistryRegistries}${resourceToken}'
    location: location
    acrAdminUserEnabled: true
    tags: tags
    publicNetworkAccess: 'Enabled'
    roleAssignments:[
      {
        principalId: dbchatProIdentity.outputs.principalId
        principalType: 'ServicePrincipal'
        roleDefinitionIdOrName: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
      }
    ]
  }
}

// Container apps environment
module containerAppsEnvironment 'br/public:avm/res/app/managed-environment:0.4.5' = {
  name: 'container-apps-environment'
  params: {
    logAnalyticsWorkspaceResourceId: monitoring.outputs.logAnalyticsWorkspaceResourceId
    name: '${abbrs.appManagedEnvironments}${resourceToken}'
    location: location
    zoneRedundant: false
  }
}

module dbchatProIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.2.1' = {
  name: 'dbchatProidentity'
  params: {
    name: '${abbrs.managedIdentityUserAssignedIdentities}dbchatPro-${resourceToken}'
    location: location
  }
}

module dbchatProFetchLatestImage './modules/fetch-container-image.bicep' = {
  name: 'dbchatPro-fetch-image'
  params: {
    exists: dbchatProExists
    name: 'dbchat-pro'
  }
}

var dbchatProAppSettingsArray = filter(array(dbchatProDefinition.settings), i => i.name != '')
var dbchatProSecrets = map(filter(dbchatProAppSettingsArray, i => i.?secret != null), i => {
  name: i.name
  value: i.value
  secretRef: i.?secretRef ?? take(replace(replace(toLower(i.name), '_', '-'), '.', '-'), 32)
})
var dbchatProEnv = map(filter(dbchatProAppSettingsArray, i => i.?secret == null), i => {
  name: i.name
  value: i.value
})

module dbchatPro 'br/public:avm/res/app/container-app:0.8.0' = {
  name: 'dbchatPro'
  params: {
    name: 'dbchat-pro'
    ingressTargetPort: 80
    scaleMinReplicas: 1
    scaleMaxReplicas: 10
    secrets: {
      secureList:  union([
      ],
      map(dbchatProSecrets, secret => {
        name: secret.secretRef
        value: secret.value
      }))
    }
    containers: [
      {
        image: dbchatProFetchLatestImage.outputs.?containers[?0].?image ?? 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'
        name: 'main'
        resources: {
          cpu: json('0.5')
          memory: '1.0Gi'
        }
        env: union([
          {
            name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
            value: monitoring.outputs.applicationInsightsConnectionString
          }
          {
            name: 'AZURE_CLIENT_ID'
            value: dbchatProIdentity.outputs.clientId
          }
          {
            name: 'PORT'
            value: '80'
          }
        ],
        dbchatProEnv,
        map(dbchatProSecrets, secret => {
            name: secret.name
            secretRef: secret.secretRef
        }))
      }
    ]
    managedIdentities:{
      systemAssigned: false
      userAssignedResourceIds: [dbchatProIdentity.outputs.resourceId]
    }
    registries:[
      {
        server: containerRegistry.outputs.loginServer
        identity: dbchatProIdentity.outputs.resourceId
      }
    ]
    environmentResourceId: containerAppsEnvironment.outputs.resourceId
    location: location
    tags: union(tags, { 'azd-service-name': 'dbchat-pro' })
  }
}
// Create a keyvault to store secrets
module keyVault 'br/public:avm/res/key-vault/vault:0.6.1' = {
  name: 'keyvault'
  params: {
    name: '${abbrs.keyVaultVaults}${resourceToken}'
    location: location
    tags: tags
    enableRbacAuthorization: false
    accessPolicies: [
      {
        objectId: principalId
        permissions: {
          secrets: [ 'get', 'list' ]
        }
      }
      {
        objectId: dbchatProIdentity.outputs.principalId
        permissions: {
          secrets: [ 'get', 'list' ]
        }
      }
    ]
    secrets: [
    ]
  }
}
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = containerRegistry.outputs.loginServer
output AZURE_KEY_VAULT_ENDPOINT string = keyVault.outputs.uri
output AZURE_KEY_VAULT_NAME string = keyVault.outputs.name
output AZURE_RESOURCE_DBCHAT_PRO_ID string = dbchatPro.outputs.resourceId
