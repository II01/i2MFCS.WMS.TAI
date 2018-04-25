delete from [Logs]
delete from [Commands]
delete from [Orders]
delete from [CommandERPs]
delete from [Places]
delete from [TUs]
delete from [TU_ID]
delete from [SKU_ID]

select
	(select count(*) from [Logs]) as Logs,
	(select count(*) from [Commands]) as Commands,
	(select count(*) from [Orders]) as Orders,
	(select count(*) from [CommandERPs]) as CommandERPs,
	(select count(*) from [Places]) as Places,
	(select count(*) from [TUs]) as TUs,
	(select count(*) from [TU_ID]) as TU_ID,
	(select count(*) from [SKU_ID]) as SKU_ID
