
DECLARE @JSON VARCHAR(MAX);

SELECT @JSON = BulkColumn
FROM OPENROWSET 
(BULK 'index_2019.json', 
DATA_SOURCE = 'MyAzureBlobStorage', SINGLE_BLOB, FORMATFILE_DATA_SOURCE = 'MyAzureBlobStorage') 
AS j;

INSERT INTO IRSNonProfitDocuments
SELECT EIN, [URL], DLN, TaxPeriod, FormType, NEWID() AS Id
  FROM OPENJSON (@JSON, '$.Filings2019')
  WITH (EIN VARCHAR(20) '$.EIN',
    [URL] VARCHAR(200) '$.URL',
	OrganizationName VARCHAR(100) '$.OrganizationName',
	DLN VARCHAR(50) '$.DLN',
    TaxPeriod VARCHAR(10) '$.TaxPeriod',
    FormType VARCHAR(10) '$.FormType') as e
	WHERE EXISTS ((SELECT 1 FROM IRSNonProfits as a WHERE (a.EIN = e.EIN)))