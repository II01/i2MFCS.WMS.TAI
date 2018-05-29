-- you can change date

declare @starttime DateTime = '2018-05-28 10:00:00'
declare @endtime DateTime = '2018-05-28 19:00:00'

-- don't change code below

use [i2MFCS.WMS.Database.Tables.WMSContext]

select 
palletsIn = 
isnull(
	(select count(*) as toWarehouse from [Commands]	
	where (Source = 'T014' and Target like 'W:1%' or Target like 'W:2%') and Status=3 and
	LastChange > @starttime and LastChange < @endtime), 0),
palletsOut = 
isnull(
	(select count(*) as toOutput from [Commands]
	where (Target like 'W:32%' or Target like 'T04%') and Status=3 and 
	LastChange > @starttime and LastChange < @endtime ), 0)

select ii.date, ii.hour, isnull(ii.num,0) 'palletsIn', isnull(oo.num,0) 'palletsOut', isnull(ii.num+oo.num, 0) 'palletsInOut'
from 
(
	select cast(lastchange as date) 'date', DATEPART(hour, lastchange) 'hour', count(*) as 'num'
	from [Commands]
	where (Source = 'T014' and Target like 'W:1%' or Target like 'W:2%') and Status=3 and
			LastChange > @starttime and LastChange < @endtime
	GROUP BY cast(lastchange as date), DATEPART(hour, lastchange)
) ii
full join
(
	select cast(lastchange as date) 'date', DATEPART(hour, lastchange) 'hour', count(*) as 'num' 
	from [Commands]
	where (Target like 'W:32%' or Target like 'T04%') and Status=3 and 
			LastChange > @starttime and LastChange < @endtime
	GROUP BY cast(lastchange as date), DATEPART(hour, lastchange)
) oo
on ii.date = oo.date and ii.hour = oo.hour
