# Encrypted File Vault  

A **password-protected encrypted storage system** for securely storing and managing sensitive files.  

## **Features**  
✔ **Military-Grade Encryption** – AES-256-CBC with unique IVs per file  
✔ **Secure Password Storage** – Argon2id for key derivation (resistant to brute-force attacks)  
✔ **Vault Management** – Create, delete, and access encrypted vaults  
✔ **File Operations** – Add, extract, and securely delete files  
✔ **Metadata Protection** – Original filenames and timestamps stored securely  

## **Technical Details**  
- **Crypto**: Argon2id (KDF) + AES-256-CBC (Encryption)  
- **Storage**: Encrypted files with prepended IVs for security  
- **UI**: WPF-based desktop application  
- **Data Protection**: Salting, constant-time hash comparison  

## **How to Use**  
1. **Create a Vault**: Set a strong password to initialize encrypted storage.  
2. **Add Files**: Drag and drop files—they’re encrypted immediately.  
3. **Extract Files**: Decrypt files to a secure location.  
4. **Delete Vaults**: Permanently erase all encrypted data.  

## **Security Considerations**  
✅ **No Plaintext Passwords** – Passwords are hashed immediately  
✅ **Unique Salts & IVs** – Prevents rainbow table and replay attacks  

## **Setup**  
Requires:  
- .NET 6+  
- Konscious.Security.Cryptography (Argon2)  

Clone, build, and run via Visual Studio.  

### Author
Cezar Pedroso
