# DBChatPro

DBChatPro is an application designed to facilitate seamless communication with your database. It allows users to interact with their database using natural language queries, making database management and data retrieval more intuitive and user-friendly.

## Prerequisites

Before you begin, ensure you have met the following requirements:

- The [.NET 8.0 SDK](https://dotnet.microsoft.com/download) installed locally
- You have access to one or more connection strings for a compatible database (SQL Server, MySQL, PostgreSQL, Oracle)
- You have access keys or identity access configured for one or more of the supported AI platforms:
    - Azure OpenAI
    - OpenAI
    - Ollama
    - AWS Bedrock
    - GitHub AI Models

## Download and open the app

1. To download the app onto your local computer:

    - In an empty directory, run the following git command:

    ```sh
    git clone https://github.com/alex-wolf-ps/dbchatpro.git
    ```

    OR

    Download the zipped project from the GitHub repo page.

1. To open the project:
    - In Visual Studio, double click on the `DbChatPro.sln` solution file in the root of the project.
    - In Visual Studio Code, right click in the root folder of the project you downloaded and select `Open with Code`.

## Configure the AI connections

You'll need to authenticate to one of the supported AI platforms to use the app. To configure a connection to your desired AI platform, provide a value for one or more of the following settings in the `appsettings.json` file:

    ```json
    "AZURE_OPENAI_ENDPOINT": "", 
    "OPENAI_KEY": "",
    "OLLAMA_ENDPOINT": "",
    "GITHUB_MODELS_KEY": "",
    "AWS": {
        "Region": "",
        "Profile": ""
    }
    ```

## Run the app

After you configure your AI connections, run the app using one of the following options:

- Press the green **Start** button at the top of Visual Studio.
- Run the `dotnet run` command from a terminal window open to the root of the project.

Open your browser and navigate to `http://localhost:5000` to access the application.

## Create a database connection

Configure a connection to your database through the UI of the app to begin querying your data.

1. Navigate to the **Connections** page of the app.
1. Fill out the form to add a connection:
    - Select your database platform of choice.
    - Enter a name for the connection.
    - Paste in a connection string to your database. The database connection string must be in the formats listed in the next section for the respective database platform.
1. Click **Check Connection** to analyze the schema of your database.
1. If you're happy with the results, select **Save Schema**.

### Sample database connection string formats

- **SQL Server**:
    ```plaintext
    Data Source=localhost;Initial Catalog=<database-name>;Trusted_Connection=True;TrustServerCertificate=true
    ```

- **MySQL**:
    ```plaintext
    Server=127.0.0.1;Port=3306;Database=<database-name>;Uid=<username>;Pwd=<password>
    ```

- **PostgreSQL**:
    ```plaintext
    Host=127.0.0.1;Port=5432;Database=<database-name>;Username=<username>;Password=<password>
    ```

- **Oracle**:
    ```plaintext
    User Id=<your-username>;Password=<your-password>;Data Source=<host>:<port>/<service-name>
    ```

## Azure Developer CLI support

This repository is structured as an Azure Developer CLI template to support automated provisioning and deployment of resources to Azure. 

- **This scenario only works for deploying to Azure and with the Azure OpenAI service.**
- **This scenario may result in some costs, as Azure resources are provisioned..**

The Azure Developer CLI will automatically provision the following:

- Azure OpenAI Service
- Azure Container Apps
- Azure Key Vault
- Azure Storage
- Azure Resource Group

It will also automatically deploy your app to a Container Apps instance.

### Set up AZD

1. Install the [Azure Developer CLI](https://aka.ms/azure-dev/install-azd):

    ```sh
    winget install microsoft.azd
    ```

    OR

    ```sh
    curl -fsSL https://aka.ms/install-azd.sh | bash
    ```

1. Authenticate to Azure using azd:

    ```sh
    azd auth login
    ```

### Provision and deploy the app resources

1. In a terminal open to the root of your project:

    ```sh
    azd init
    ```

1. Provision and deploy your resources:

    ```sh
    azd up
    ```

1. The deployment process may take some time. Once the deployment is complete, you will see the URL where your application is hosted. Open this URL in your browser to access the application.

1. By default the Container App has ingress disabled to keep the app private. You'll need to enable ingress in the container app settings.

For more detailed information on using the Azure Developer CLI, refer to the [official documentation](https://aka.ms/azure-dev/overview).
