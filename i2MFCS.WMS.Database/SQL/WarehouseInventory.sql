use [i2MFCS.WMS.Database.Tables.WMSContext]

select TUs.SKU_ID, TUs.Batch, TUs.Qty, TUs.ProdDate, TUs.ExpDate, right('000000000' + convert(nvarchar(9), Places.TU_ID), 9), Places.PlaceID from [Places]
join TUs on Places.TU_ID = TUs.TU_ID
where Places.PlaceID like 'W:1%' or Places.PlaceID like 'W:2%'
order by SKU_ID asc, Batch asc

