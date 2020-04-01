
Log("spoustim script");

State_stand = {};
State_attack = {};
State_run = {};
State_heal = {};
State_round = {};
State_step = {};
State_go = {};
	

--attach npc and set variables
function Init()
	npc = GetNpc();
	summoned = false;
	--healCooldown = 0;
	--healRechargeTime = 4000;

	actualState = State_stand
end

--update function
function Update(miliseconds)
	actualState.Execute()
end


--State Stand
State_stand["Execute"] = function()
	local status = GetStatus();
	--Log(status.enemySeen);
	if status.enemySeen > 0 then
		ToAttack();
		return;
	elseif HasTask() == true then
		ToGo();
	end
end

State_stand["Enter"] = function()
	Log("state stand... entering");
end

State_stand["Exit"] = function()
	Log("state stand... exiting");
end


--State Attack
State_attack["Execute"] = function()
	local status = GetStatus();
	
	if status.enemySeen == 0 then
		ToStand();		
	elseif status.hp < npc.character.hp / 4 and not IsMoving() then
		ToRun();
	--elseif not IsMoving() then
		--r = math.random(100);
		--if r < 5 then
			--ToRound();
		--else
			--updatePosition();
		--end
	else
		action = getBestAction();
		Log("Provedu: " ..action);
		if action == "Heal" then
			ToHeal();
		else
			Spell(action);
		end;
		if not IsMoving() then
			updatePosition();
		end
	end
end

State_attack["Enter"] = function()
	Log("state attack... enter");
	--Talk("Jdu po Tobe!!");
end

State_attack["Exit"] = function()
	Log("state attack... exiting");
end


--State Heal
State_heal["Execute"] = function()
	Talk("--Heal--");
	Spell("Heal");
	--healCooldown = healRechargeTime;
	ToAttack();
end

State_heal["Enter"] = function()
	Log("state heal... enter");
end

State_heal["Exit"] = function()
	Log("state heal... exiting");
end

--State step
State_step["Execute"] = function()
	local status = GetStatus();
	local enemyStatus = GetEnemyStatus();
	
	x,y = status.position.X, status.position.Y;
	enemyX, enemyY = enemyStatus.position.X, enemyStatus.position.Y;
		
	distance = calculateDistance(x, y, enemyX, enemyY);
	
	if distance > 7 then
		dirX, dirY = enemyX + x, enemyY + y;
	else
		dirX, dirY = enemyX - x, enemyY - y;
	end
		
	--normalize
	if dirX ~= 0 then	
		dirX = dirX/math.abs(dirX);
	end
	if dirY ~= 0 then
		dirY = dirY/math.abs(dirY);
	end
	targetX, targetY = x+dirX, y+dirY;
	GoAt(targetX, targetY);
	ToAttack();
end

State_step["Enter"] = function()
	Log("state step... enter");
end

State_step["Exit"] = function()
	Log("state step... exiting");
end


--State Run
State_run["Execute"] = function()
	Log("state run... executing");
	local status = GetStatus();
	local enemyStatus = GetEnemyStatus();
	
	x,y = status.position.X, status.position.Y;
	enemyX, enemyY = enemyStatus.position.X, enemyStatus.position.Y;
	
	--direction where to run away from enemy
	dirX, dirY = enemyX - x, enemyY - y;
	
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
	Log("state run ... entering");
	--Talk("Nademnou nevyhrajes!!");
end
			
State_run["Exit"] = function()
	Log("state run... exiting");
end

--State round
State_round["Execute"] = function()
	local status = GetStatus();
	local enemyStatus = GetEnemyStatus();
	x,y = status.position.X, status.position.Y;
	enemyX, enemyY = enemyStatus.position.X, enemyStatus.position.Y;
	diffX, diffY = enemyX - x, enemyY - y;
	
	--don't go too far
	if math.abs(diffX) > 7 then
		diffX = diffX/2;
	end	
		
	if math.abs(diffY) > 7 then
		diffY = diffY/2;		
	end
	
	targetX, targetY = enemyX + diffX, enemyY + diffY;
	
	GoAt(targetX, targetY);
	ToAttack();
end

State_round["Enter"] = function()
	Log("state round ... entering");
end
			
State_round["Exit"] = function()
	Log("state round... exiting");
end

--State go
State_go["Execute"] = function()
	if HasTask == false then
		ToStand();
	else
		Go();
	end
end
State_go["Enter"] = function()
	
end
State_go["Exit"] = function()
end

function updatePosition()
	local status = GetStatus();
	local enemyStatus = GetEnemyStatus();
	
	x,y = status.position.X, status.position.Y;
	enemyX, enemyY = enemyStatus.position.X, enemyStatus.position.Y;
		
	distance = calculateDistance(x, y, enemyX, enemyY);
	
	if distance > 7 then
		dirX, dirY = enemyX + x, enemyY + y;
	else
		dirX, dirY = enemyX - x, enemyY - y;
	end
		
	--normalize
	if dirX ~= 0 then	
		dirX = dirX/math.abs(dirX);
	end
	if dirY ~= 0 then
		dirY = dirY/math.abs(dirY);
	end
	targetX, targetY = x+dirX, y+dirY;
	GoAt(targetX, targetY);
	--ToAttack();
end


function getBestAction()
	local status = GetStatus();
	local enemyStatus = GetEnemyStatus();
	
	actions = {};
	costs = {};
	fSum, hSum, mSum = 0,0,0;
	
	if isValidAction("Fireball") then
		fireball = GetAction("Fireball");
		table.insert(actions, "Fireball");
		
		f = {0, enemyStatus.hp - fireball.enemyHpTaken, fireball.manaDrain}			
		for k,v in pairs(f) do
			fSum = fSum + v;
		end
		table.insert(costs, fSum);
	end
	if isValidAction("Heal") then
		heal = GetAction("Heal");
		table.insert(actions, "Heal");
		
		h = {status.hp + heal.hpGot, 0, heal.manaDrain};
		for k,v in pairs(h) do
			hSum = hSum + v;
		end
		table.insert(costs, hSum);
	end
	if isValidAction("Moon prayer") then
		mana = GetAction("Moon prayer");
		table.insert(actions, "Moon prayer");
		
		m = {0,0,status.mana};
		for k,v in pairs(m) do
			mSum = mSum + v;
		end
		table.insert(costs, mSum);
	end
	
	minimum = 99999999;
	result = "Moon prayer";
	
	for index, cost in ipairs(costs) do
		Log(actions[index] .." cost=" ..cost);
		if cost < minimum then
			result = actions[index];
			minimum = cost;
		end
	end

	Log("akce: " ..result);
	return result;
end

function isValidAction(actionName)
	local status = GetStatus();
	local action = GetAction(actionName);
	
	if status.mana - action.manaDrain < 0 then 
		return false;
	elseif status.mana - action.manaDrain > npc.character.mana then
		return false;
	elseif status.hp + action.hpGot > npc.character.hp then
		return false;
	end
	
	return true;
end


function calculateDistance(x1,y1,x2,y2)
	local dx = math.abs(x1-x2);
	local dy = math.abs(y1-y2);
	return math.max(dx,dy);
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

function ToRound()
	actualState.Exit();
	actualState = State_round;
	actualState.Enter();
end

function ToStep()
	actualState.Exit();
	actualState = State_step;
	actualState.Enter();
end

function ToGo()
	actualState.Exit();
	actualState = State_go;
	actualState.Enter();
end