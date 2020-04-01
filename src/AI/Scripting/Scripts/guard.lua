
Log("lua: spoustim script");

State_stand = {};
State_attack = {};
State_run = {};
State_heal = {};
State_mana = {};
	

--attach npc and set variables
function Init()
	npc = GetNpc();
	actualState = State_stand;
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
	elseif status.hp < npc.character.hp / 5 and not alreadyRunned and not IsMoving() then
		ToRun();
	else
		if status.hp < npc.character.hp/4 and isValidAction("Heal") then
			ToHeal();
		elseif isValidAction("Fireball") then 				
			Spell("Fireball");
		else					--not enough mana
			ToMana();
		end

	end
end

State_attack["Enter"] = function()
	Log("LUA: state attack... enter");
end

State_attack["Exit"] = function()
	Log("LUA: state attack... exiting");
end


--State Heal
State_heal["Execute"] = function()
	Spell("Heal");
	ToAttack();
end

State_heal["Enter"] = function()
	Log("LUA: state heal... enter");
end

State_heal["Exit"] = function()
	Log("LUA: state heal... exiting");
end

--State mana
State_mana["Execute"] = function()
	Spell("Moon prayer");
	ToAttack();
end

State_mana["Enter"] = function()
	Log("LUA: state heal... enter");
end

State_mana["Exit"] = function()
	Log("LUA: state heal... exiting");
end


--State Run
State_run["Execute"] = function()
	Log("LUA: state run... executing");
	local status = GetStatus();
	local enemyStatus = GetEnemyStatus();
	
	x,y = status.position.X, status.position.Y;
	enemyX, enemyY = enemyStatus.position.X, enemyStatus.position.Y;
	
	--direction where to run away from enemy
	dirX, dirY = enemyX + x, enemyY + y;
	
	--normalize
	if dirX ~= 0 then	
		dirX = dirX/math.abs(dirX);
	end
	if dirY ~= 0 then
		dirY = dirY/math.abs(dirY);
	end
	
	targetX, targetY = x + npc.character.visualRange * dirX, y + npc.character.visualRange * dirY;
	GoAt(targetX, targetY);
	ToAttack();
end

State_run["Enter"] = function()
	Log("LUA: state run ... entering");
	Talk("Jeste jsme neskoncili");
end
			
State_run["Exit"] = function()
	Log("LUA: state run... exiting");
end

function isValidAction(actionName)
	local status = GetStatus();
	local action = GetAction(actionName);
	
	if status.mana < action.manaDrain then 
		return false;
	end
	
	return true;
	
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

function ToHeal()
	actualState.Exit();
	actualState = State_heal;
	actualState.Enter();
end

function ToMana()
	actualState.Exit();
	actualState = State_mana;
	actualState.Enter();
end

