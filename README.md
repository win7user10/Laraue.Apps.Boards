# Laraue.Apps.Boards
The backend for Task-management system.

### Matter decisions and why they were taken
#### About nullable identifier
##### There should be also default space in each organization

## App layers
Below is explanation about project structure

### Laraue.Apps.StructuredMessages.DataAccess
The layer that contains models and enums associated with these models

## Testing

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