# Chemotion URI schema

Chemotion URI schema is meant to bridge the gap between browser and desktop applications. A common use case would be accessing and editing files stored in the ELN with desktop-installed applications.
While one could simply download the files from the ELN and then open the file with a specific application the goal is to simplify the process to only require a single click.

The idea would be to install a URI schema handler on the client system and generating clickable links in the lab journal. The handler can then, for example, download data, launch the desired application, wait for changes and reupload the data afterwards. A simplified example can be seen here: [Youtube](https://www.youtube.com/watch?v=da_2mAzXj_Y)


## Implications

While technically there seem to be only low hurdles on realizing such a project, a few things have to be considered:

### Authentication

If the URI handler needs to access data on the ELN, the handler needs to authenticate to the Chemotion server. This can be achieved in several ways:

1. interactive auth: the handler asks the user for credentials. This deserves the purpose of being a one-click solution
2. static authentication for the handler: the handler is preconfigured on a per-machine, per-user basis
3. shared authentication with the browser: the handler receives the authentication from a browser session. This could for example be done by encoding a secret into the URI passed to the handler.

Option 1 does defeat the purpose as the user would have to enter the credentials at least once per session.
Option 2 would require the a user to manage credential files among several machines and user accounts, similar to managing SSH keys. This could be effortful process for a user if several machines are used and requires to _remember_ all machines their keys are deployed at.
Option 3 requires the browser to pass it's authentication forward to the URI handler. This could be done with an API token that's encoded in the URI. To prevent abuse, it should be considered that the API token does not have comprehensive permissions but only the ones needed for the desired operation and be as specific as possible, preferable in the form of:

```
Generic content: [action]  [resource]  [how often]  [timeframe (from:to)]
                 ┌──┴───┐  ┌────┴──┐     ┌─┴┐       ┌──────────┴────────────┐
Example token:   download  fileID123     once       within the next 2 minutes
```

### URI schema design

A suggestd URI design could be:

`chemotion:[endpoint]#[action]/[resource Identifier]?token=[token]`

break down:

| **Token**             | **Description**                                                     | **Notes**                                              |
|-----------------------|---------------------------------------------------------------------|--------------------------------------------------------|
| chemotion://          | constant string to identify the schema                              |                                                        |
| [endpoint]            | URL of the Chemotion journal                                        | Example could be "https://chemotion.edu:3000/"         |
| [action]              | A string expressing an action                                       | This could for example be "download", "upload", "edit" or "open" |
| [resource identifier] | A unique identifier referencing an object in Chemotion.             | This could i.e. be the internal ID of a file           |
| [token]               | self-describing token granting access for the respective operations | JSON object could look like shown below.               |


```ja=
// auth token could be a standard JWT, containing the information below as payload (beside some standard information such as alg, typ):

let payload = {
    name: "<string:owner>",
    rules: [
        <rule:rule1>,
        <rule:rule2>,
        <rule:rule3>,
        ...
    ]
}

// where the rule type is the following:
let rule = {
    resource: "<string:resource identifier>",
    action: "<string:action>",
    validity: {
        from: <int:timestamp>,
        until: <int:timestamp>,
        uses: <int:amount>
    }
}
```

## Example:

- User accesses Chemotion using the browser
- Browser requests the website from the server
- Server replies with `chemotion://` links with encoded tokens, i.e. action=edit, resource=file123
- User clicks on the links, custom handler receives the URI
- handler decodes the URI and uses the token to download the file123, then starts the associated program on the client computer
- as soon as the associated program is terminated, the handler uses the token to upload the edited file back to the Chemotion server
- server invalidates then token

## Resources & Actions

> Here a list of possible resources and their accepted actions can follow. For now this is limited to file handling.

Possible resources could be:

- `Files`

Actions on `Files`:

- `download`: downloads a file from the Chemotion ELN
- `upload`: uploads a file to the ELN replacing the old version
- `edit`: downloads a file and uploads it after change
- `open`: downloads a file and opens it using the system default app. file should be marked as read-only, as no no upload happens afterwards.

## Additional Notes:

- to simplify the registration of the URI handler, the executable should accept a "--register" flag which then registers the executable as a `chemotion://`-schema handler.
