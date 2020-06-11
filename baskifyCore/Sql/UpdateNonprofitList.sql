INSERT INTO IRSNonProfits(EIN, OrganizationName, City, [State], Country)
SELECT EIN, OrganizationName, City, [State], Country
  FROM OPENROWSET(
	BULK  'data-download-pub78.txt',
	DATA_SOURCE = 'MyAzureBlobStorageRoot',
	FIRSTROW = 3,
	LASTROW = 1167122,
	FORMATFILE='format.fmt',
	FORMATFILE_DATA_SOURCE = 'MyAzureBlobStorageRoot'
  ) AS Datafile