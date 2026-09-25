# Activer le hook pre-commit

Le hook Git est configure dans le depot avec Husky.Net. Il doit etre active une
fois sur chaque clone afin que Git l'execute avant chaque commit.

## Prerequis

- Le SDK .NET indique dans `global.json` est installe.
- Le depot a ete clone localement.

## Installation

Depuis la racine du depot, executer :

```powershell
dotnet tool restore
dotnet husky install
```

`dotnet tool restore` installe les outils locaux declares dans
`dotnet-tools.json`, dont Husky.Net. `dotnet husky install` configure le chemin
des hooks Git pour ce clone. Cette configuration est locale et n'est pas
versionnee : chaque developpeur doit effectuer cette etape.

## Verification

Verifier que Git utilise bien le repertoire des hooks :

```powershell
git config --get core.hooksPath
```

La commande doit afficher `.husky`. Pour tester le hook sans creer de commit :

```powershell
git hook run pre-commit
```

Sans fichier C# stage, les taches sont affichees comme ignorees : c'est le
comportement attendu.

## Comportement

Avant chaque commit, le hook :

1. applique `dotnet format whitespace` aux fichiers `.cs` stages ;
2. ajoute de nouveau au stage les fichiers eventuellement reformates.

Le hook ne couvre volontairement que le formatage des fichiers C# modifies. La
verification complete du formatage, le build et les tests restent executes par
la CI.

Le hook `commit-msg`, installe au meme moment, verifie aussi que le message
respecte les Conventional Commits. Les conventions et les controles CI sont
documentes dans [GIT_RULES.md](./GIT_RULES.md).

## Depannage

Si `husky` est introuvable, relancer `dotnet tool restore`. Si le hook n'est pas
execute par Git, relancer `dotnet husky install` depuis la racine du depot puis
verifier `core.hooksPath`.

En cas de besoin exceptionnel, un commit peut ignorer les hooks avec
`git commit --no-verify`. Les controles obligatoires restent appliques en CI.
