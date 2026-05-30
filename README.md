# Laraue.Apps.Boards
The repository contains backend for Task-management Jira-like system.

## App structure

### Laraue.Apps.StructuredMessages.DataAccess
The layer that contains models and enums associated with these models

### Laraue.Apps.StructuredMessages.Services
The layer with services that use other services. Required to encapsulate hard logic mostly in CRD operations.  
Example: issue creation may update `updated_at` property, add record to history changes etc.  
So the service provides the method to create issue.

**Note:** core services should not manage transactions, but may require them calling `context.Database.EnsureTransaction`
at the top of function.

### Laraue.Apps.StructuredMessages.WebApiServices
Services to call from WebApi

### Laraue.Apps.StructuredMessages.TelegramServices
Services to call from TelegramApi

## Testing
Check how to deal with the frontend in [Frontend Repository](https://github.com/win7user10/laraue-note-to-board)

### Create a new user for Test
`POST: http://localhost:5200/api/test/user`
```json
{
    "username": "winDiezel",
    "languageCode": "ru",
    "firstName": null,
    "lastName": null
}
```

### Auth as user on Test
Make a request GET `http://localhost:5200/api/test/user/{userId}` to receive a bearer token.  
Set it in frontend `.env` file:
```
NUXT_PUBLIC_TEST_USER_TOKEN=Taken_Token
```