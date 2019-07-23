pipeline {
  agent {
    docker {
      image 'microsoft/dotnet:2.2-sdk'
    }
  }
  stages {
    stage('Run tests') {
      steps {
        sh 'dotnet test . -l:trx'
      }
    }
  }
  environment {
    HOME = '/tmp'
  }
  post {
    always {
      mstest(testResultsFile: 'MessageBroker.Tests/TestResults/*.trx')
    }
  }
}