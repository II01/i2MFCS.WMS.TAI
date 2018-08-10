-- copy to historders
insert into dbo.HistOrders (ID, ERP_ID, OrderID, SubOrderID, SKU_ID, SubOrderName, SKU_Qty, Destination, ReleaseTime, SKU_Batch, Status, SubOrderERPID)
select ID, ERP_ID, OrderID, SubOrderID, SKU_ID, SubOrderName, SKU_Qty, Destination, ReleaseTime, SKU_Batch, Status, SubOrderERPID
from dbo.Orders
where Status >= 4

-- copy to histcommands
insert into dbo.HistCommands (ID, Order_ID, TU_ID, Source, Target, Status, Time, LastChange)
select ID, Order_ID, TU_ID, Source, Target, Status, Time, LastChange
from dbo.Commands
where Status >= 2

-- delete commands 
delete from dbo.Commands
where Status >= 2

-- delete orders
delete from dbo.Orders
where Status >= 4