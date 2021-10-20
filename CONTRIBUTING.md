# Contribuer au projet

## Prérequis

- .NET Core 3.1 [lien](https://dotnet.microsoft.com/download)
- Visual Studio Code [lien](https://code.visualstudio.com/)

### NOTES

N'oubliez pas de désactiver la télémétrie de .NET Core:

- Windows:

```sh
setx DOTNET_CLI_TELEMETRY_OPTOUT 1
```

- UNIX:

```sh
echo "DOTNET_CLI_TELEMETRY_OPTOUT=1" | sudo tee -a /etc/environment
```

Pour une meilleure gestion des dépendances NuGet, installez ceci:

```sh
dotnet tool install --global dotnet-outdated
```

Puis, positionnez-vous dans le répertoire du projet dont vous voulez mettre à jour les dépendances et tapez:

```sh
dotnet-outdated -u
```

### Extensions conseillées

- C# [lien](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp)
- Code Runner [lien](https://marketplace.visualstudio.com/items?itemName=formulahendry.code-runner)
- C# Extensions [lien](https://marketplace.visualstudio.com/items?itemName=jchannon.csharpextensions)
- C# XML Documentation Comments [lien](https://marketplace.visualstudio.com/items?itemName=k--kato.docomment)
- GitHub Pull Requests [lien](https://marketplace.visualstudio.com/items?itemName=GitHub.vscode-pull-request-github)
- vscode-solution-explorer [lien](https://marketplace.visualstudio.com/items?itemName=fernandoescolar.vscode-solution-explorer)
- Git Extension Pack [lien](https://marketplace.visualstudio.com/items?itemName=donjayamanne.git-extension-pack)

## Travailler avec la base de données

Nous utilisons PostgreSQL comme système de gestion de base de données.
En effet, bien plus solide que MySQL, il dispose d'un excellent support dans Azure Data Studio, l'application client qu'on vous conseille d'utiliser.

- Téléchargez Postgres [ici](https://www.postgresql.org/download/)
- Téléchargez Azure Data Studio [ici](https://docs.microsoft.com/en-us/sql/azure-data-studio/download?view=sql-server-2017)
- Installez l'extension PostgreSQL: [tuto](https://docs.microsoft.com/en-us/sql/azure-data-studio/postgres-extension?view=sql-server-2017)

### Se connecter avec Azure Data Studio

Lancez Azure Data Studio et cliquez sur l'icône "New Connection" à droite de "SERVERS".
Choisissez "PostgreSQL" comme nom de connexion et connectez-vous avec les mêmes identifiants que vous avez créés lorsque vous avez installé Postgres.

### Ajouter une migration et mettre à jour la base de données

Positionnez-vous dans le répertoire du projet "Forum.Data" et tapez:

```sh
dotnet ef migrations add "nom de la migration" -s ../Forum/Forum.csproj
dotnet ef database update -s ../Forum/Forum.csproj
```

### Transférer la base de données avec PostgreSQL

Pour effectuer une sauvegarde au format SQL de la base de données, positionnez-vous dans le répertoire d'installation de Postgres
(par défaut : C:/Program Files/PostgreSQL/<numero_version>/bin) et tapez :

```sh
./pg_dump.exe -U postgres -d Forum -f C:/Users/<nom_utilisateur>/Desktop/DumpV2.sql
```

Pour importer le fichier SQL de sauvegarde dans PostgreSQL, commencez par créer une base de données vierge dans Azure Data Studio:

```sql
CREATE DATABASE Forum;
```

Puis, positionnez-vous dans le même répertoire tel que décrit précédemment, et tapez:

```sh
./psql.exe --host "localhost" --port "5432" --username "postgres" -d Forum -f "C:\\Users\\<nom_utilisateur>\\Desktop\\DumpV2.sql"
```

/!\ ATTENTION
Des erreur de clés étrangères ou clés déjà existantes peuvent apparaître. N'ayez crainte, cela n'affecte aucunement la migration.
Pour réintroduire ces contraintes, il suffit d'ouvrir le fichier "DumpV2.sql", copier les contraines définies en toute fin de fichier,
et de les exectuter dans Azure Data Studio.

## Documentation

La documentation est générée automatiquement à la compilation. Pour y accèder, il suffit de se rendre à l'adresse:

<https://localhost:5001//docs/>

Toute la documentation n'a pas encore été rédigée.
Pour ce faire, positionnez-vous juste au dessus de la signature de la méthode dont vous souhaitez écrire la documenation et tapez 3 "/".
VS Code imprimera alors un modèle de documentation pour la méthode concernée tel que :

```xml
/// <summary>
///
/// </summary>
/// <param name="model"></param>
/// <returns></returns>
```

Il suffit ensuite de suivre le modèle indiqué
