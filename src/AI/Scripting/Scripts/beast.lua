
Log("lua: spoustim script");

State_stand = {};
State_attack = {};
State_run = {};
	

--attach npc and set variables
function Init()
	npc = GetNpc();
	actualState = State_stand
	alreadyRunned = false;
end

--update function
function Update(miliseconds)

	actualState.Execute()
end


--State Stand
State_stand["Execute"] = function()
	local status = GetStatus();
	
	if status.enemySeen > 0 then
		ToAttack();
	end
end

State_stand["Enter"] = function()
	Log("LUA: state stand... entering");
end

State_stand["Exit"] = function()
	Log("LUA: state stand... exiting");
	Talk("Jdu po Tobe!!");
end


--State Attack
State_attack["Execute"] = function()
	local status = GetStatus();
	
	if status.enemySeen  == 0 then
		ToStand();	
	elseif status.hp < npc.character.hp / 4 and not alreadyRunned then
		ToRun();
	else
		Spell("Fire attack");
	end
end

State_attack["Enter"] = function()
	Log("LUA: state attack... enter");
end

State_attack["Exit"] = function()
	Log("LUA: state attack... exiting");
end



--State Run
State_run["Execute"] = function()
	Log("LUA: state run... executing");
	local status = GetStatus();
	local enemyStatus = GetEnemyStatus();
	
	x,y = status.position.X, status.position.Y;
	enemyX, enemyY = enemyStatus.position.X, enemyStatus.position.Y;
	dirX, dirY = enemyX - x, enemyY - y;
	
	--normalize
	if dirX ~= 0 then	
		dirX = dirX/dirX;
	end
	if dirY ~= 0 then
		dirY = dirY/dirY;
	end
	targetX, targetY = x + npc.character.visualRange * dirX, y + npc.character.visualRange * dirY;
	GoAt(targetX, targetY);
	alreadyRunned = true;
	ToAttack();
end

State_run["Enter"] = function()
	Log("LUA: state run ... entering");
	Talk("Jeste jsme neskoncili");
end
			
State_run["Exit"] = function()
	Log("LUA: state run... exiting");
end



--transit functions
function ToStand()
	actualState.Exit();
	actualState = State_stand;
	actualState.Enter();
end

function ToAttack()
	actualState.Exit();
	actualState = State_attack;
	actualState.Enter();
end


function ToRun()
	actualState.Exit();
	actualState = State_run;
	actualState.Enter();
end

