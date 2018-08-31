use [i2MFCS.WMS.Database.Tables.WMSContext]

delete from PlaceIDs

INSERT INTO [PlaceIDs] values 
	('C101', 0, 0, 2, 0, 0),
	('T041', 0, 0, 2, 0, 0)

declare @r int
declare @rr int
declare @x int
declare @xx int
declare @y int
declare @z int
declare @loc nvarchar(20)
declare @heightclass int
set @r = 0
while(@r < 2) begin
	set @rr = (@r/2+1) * 10 + (@r % 2+1)
	set @r = @r + 1
	set @x=0
	while(@x < 26) begin
		set @x = @x + 1
		set @y=0
		while(@y < 10) begin
			set @y = @y + 1
			if @y=4 or @y>=9
				set @heightclass=2
			else 
				set @heightclass=1
			set @z=0
			while(@z < 1) begin
				set @z = @z + 1
				set @loc = 'W:' + RIGHT('00'+CAST(@rr AS VARCHAR(2)),2) + ':' + RIGHT('00'+CAST(@x AS VARCHAR(2)),2) + ':' + RIGHT('00'+CAST(@y AS VARCHAR(2)),2) + ':' + CAST(@z AS VARCHAR(1))
				INSERT INTO [PlaceIDs] values (@loc, @x, @y, @heightclass, 0, 0)
			end
		end
	end
end

delete from PlaceIDs
where ID like 'W:11:01:%'

