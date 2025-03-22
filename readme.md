# DBChatPro

DBChatPro is an application designed to facilitate seamless communication with your database. It allows users to interact with their database using natural language queries, making database management and data retrieval more intuitive and user-friendly.

## Prerequisites

Before you begin, ensure you have met the following requirements:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download) installed locally
- One or more connection strings for a compatible database (SQL Server, MySQL, PostgreSQL, Oracle)
- You have access keys or identity access to one or more of the supported AI platforms:
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
