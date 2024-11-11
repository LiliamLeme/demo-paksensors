data "azurerm_subscription" "primary" {
}

data "azurerm_client_config" "current" {
}

data "http" "my_ip" {
  url = "https://api.ipify.org"
}

variable "resource_group_name_prefix" {
  type        = string
  default     = "rg"
  description = "Prefix of the resource group name that's combined with a random ID so name is unique in your Azure subscription."
}

variable "resource_group_name" {
    type        = string
    default     = "plsensors"
    description = "default rg name"
}

variable "resource_group_location" {
  type        = string
  default     = "eastus"
  description = "Location of the resource group."
}

variable "hubname" {
  type = string
  default = "parkinghub"
  description = "Name of the event hub"
}

resource "azurerm_resource_group" "rg" {
  name     = "${var.resource_group_name_prefix}-${var.resource_group_name}"
  location = var.resource_group_location
}

resource "random_string" "random_name" {
  length  = 5
  lower   = true
  numeric = false
  special = false
  upper   = false
}

resource "azurerm_storage_account" "storage_account" {
  resource_group_name = azurerm_resource_group.rg.name
  location            = azurerm_resource_group.rg.location

  name = "storage${random_string.random_name.result}"

  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"
  
  # Disable public access
  # public_network_access_enabled = false

  # Enable firewall rules (no access by default)
  network_rules {
    default_action             = "Deny"  # Deny all public access
    bypass                     = ["AzureServices"]  # Optional: Allow Azure services like backups to access
    ip_rules                   = [data.http.my_ip.response_body]  # Allow the current client IP only
  }
}

resource "azurerm_storage_container" "files_container" {
  name                  = "parkingsensors"
  storage_account_name  = azurerm_storage_account.storage_account.name
  container_access_type = "private"  # Private access by default
}

resource "azurerm_role_assignment" "storagecontrib" {
  scope                = azurerm_storage_container.files_container.resource_manager_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = data.azurerm_client_config.current.object_id
}


resource "azurerm_eventhub_namespace" "namespace" {
  name                = "parkingevents-${random_string.random_name.result}"
  location            = var.resource_group_location
  resource_group_name = azurerm_resource_group.rg.name
  sku                 = "Standard"
  capacity            = 1
}

resource "azurerm_eventhub" "hub" {
  name                = var.hubname
  namespace_name      = azurerm_eventhub_namespace.namespace.name
  resource_group_name = azurerm_resource_group.rg.name
  partition_count     = 2
  message_retention   = 1
}

resource "azurerm_role_assignment" "sender" {
  scope                = azurerm_eventhub.hub.id
  role_definition_name = "Azure Event Hubs Data Sender"
  principal_id         = data.azurerm_client_config.current.object_id
}

output "BlobStorageAccount" {
  value = azurerm_storage_account.storage_account.name
}

output "EventhubNamespace" {
  value = azurerm_eventhub_namespace.namespace.name
}