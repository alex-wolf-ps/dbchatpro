param identityName string 
param location string

resource acaIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
}

output principalId string = acaIdentity.properties.principalId
output identityName string = acaIdentity.name
output clientId string = acaIdentity.properties.clientId
