<?php
// inclue arquivo de conexap
session_start();
include "conexao.php";
if(empty($_POST['Email'])|| empty($_POST['senha'])){
   header('Location: index.html');
exit();
}
//variaveis com campos digitados pelos usuarios
$Email = mysqli_real_escape_string($mysqli, $_POST['Email']);
$senha = mysqli_real_escape_string($mysqli, $_POST['senha']);

//query que verifica e faz validaçoes no banco

$query = "SELECT * from cliente where Email = '$Email' and senha = md5('{$senha}')";

$result = mysqli_query($mysqli, $query);

$row = mysqli_num_rows($result);

if($row == 1){
$_SESSION['Email'] = $Email;
header('Location: painel.php');
exit();
}else{
	header('Location: login.html');
	exit();
}

?>