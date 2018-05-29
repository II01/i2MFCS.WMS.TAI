use [i2MFCS.WMS.Database.Tables.WMSContext]
select * from 
(select count(SKU_ID) as count, tu.TU_ID, pl.PlaceID
from dbo.Places as pl
left outer join dbo.TUs as tu on pl.TU_ID = tu.TU_ID
group by tu.TU_ID, pl.PlaceID) cnt
where cnt.count <> 1 and cnt.PlaceID <> 'W:out'
