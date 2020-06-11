
open master key decryption by password ='P@$$word'
go

CREATE DATABASE SCOPED CREDENTIAL MyAzureBlobStorageGlobalCredential
WITH IDENTITY = 'SHARED ACCESS SIGNATURE',
SECRET = 'sp=rl&st=2020-06-09T21:13:14Z&se=2020-06-10T21:13:14Z&sv=2019-10-10&sr=c&sig=QhjFa7CuRJ8bAX1BWoCrtpn252RPfO2wEXctfHftGxw%3D'


CREATE EXTERNAL DATA SOURCE MyAzureBlobStorageRoot
WITH ( TYPE = BLOB_STORAGE,
        LOCATION = 'https://irsdata.blob.core.windows.net/irs',
		CREDENTIAL = MyAzureBlobStorageGlobalCredential
);
