# Parking Lot Sensor Data Simulator

``` cli
az login
az account set --subscription XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
```

## Installation

From the terraform directory

``` terraform
terraform init
terraform plan
```

Verify output results then:

``` terraform
terraform apply
```

When completed, copy the name of the storage account and the eventhub namespace to appsettings.json in the application directory.

## AppSettings Explanations

Sample appsettings.json

``` json
{
    "JsonFilePath": "sensors small.json",
    "ParkingEventFrequency": 30,
    "ParkingTimeMin": 300,
    "ParkingTimeMax": 1800,
    "FileDelay": 120,
    "WriteFile": false,
    "WriteBlob": false,
    "WriteStream": false,
    "BlobStorageAccount": "<BlobStorageAccount>",
    "BlobStorageContainer": "parkingsensors",
    "FileNamePrefix": "parkinglot",
    "EventHubNamespace": "<EventhubNamespace>",
    "EventHubName": "parkinghub"
}
```

|Key|Value|
|---|-----|
|JsonFilePath | location of sensor list|
|ParkingEventFrequency | Max time between each event to park a car|
|ParkingTimeMin | Minimum time a car will be parked|
|ParkingTimeMax | Maximum time a car will be parked|
|FileDelay | Time between writing state to a file|
|WriteFile | (boolean) write a file to local storage|
|WriteBlob | (boolean) write a file to blob storage|
|WriteStream | (boolean) write each event to an event stream|
|BlobStorageAccount * | Name of the blob storage account|
|BlobStorageContainer | Name of the blog storage container|
|FileNamePrefix | prefix of the filename to be written|
|EventHubNamespace * | Namespace of the EventHub|
|EventHubName | Name of the EventHub|

> Note: (All times are in seconds)
> Note: * These values need to be updated after running terraform apply. These should be output from the script.

## Testing

[] Write Local File
    - Change "WriteFile" to true in appsettings.json
    - Verify files written to local storage
[] Write Local and Blob File
    - Change "WriteBlob" to true in appsettings.json
    - Verify files written to local and blob storage
[] Write to Stream and Local and Blob File
    - Change "WriteStream" to true in appsettings.json
    - Verify files written to local and blob storage and eventhub.
[] Write Blob File only
    - Change "WriteStream" back to false in appsettings.json
    - Change "WriteFile" back to false in appsettings.json
    - Verify files written to local storage are deleted after upload
[] Write Stream
    - Change "WriteBlob" back to false in appsettings.json
    - Verify Stream is written to
    - Verify no files written.

## Teardown

``` terraform
terraform destroy
```

[] Confirm all resources removed from Azure account
