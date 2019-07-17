import React, {useState, useEffect, useContext} from 'react'
import {AxiosService, URL} from '../../../services/AxiosService/AxiosService'
import {History} from 'history'
import {match} from 'react-router-dom'

// Components
import {DetailPageTable, ITableItem} from '../../reusables/DetailPageTable/DetailPageTable'
import {Button} from '../../reusables/Button/Button'
import {Group} from '../../reusables/Group/Group'
import placeholder from '../../../content/Images/Placeholders/program-placeholder.png'

// Utils
import {formatDate} from '../../../utilities/FormatDate'
import {format} from '../../../utilities/formatEmptyStrings'
import {formatCost} from '../../../utilities/FormatCost'

// Styles
import styles from './ProgramOverviewPage.module.css'

// Context
import {LoginContext} from '../../App/App'

// Types
interface IProgramOverviewPageProps {
    history: History
    match: match<{id: string}>
}

interface ExpectedProgramOverview {
    countProgInUse: number
    countProgOverall: number
    icon: string
    isCostPerYear: boolean
    progCostPerYear: number
    progFlatCost: number
    program: string
    programlicenseKey: string
}

export interface ExpectedProgramType {
    employeeId: number
    employeeName: string
    programId: number
    programlicenseKey: string
    renewalDate: string
}

export interface ExpectedPluginType {
    isCostPerYear: boolean
    pluginCostPerYear: number
    pluginFlatCost: number
    pluginName: string
    pluginId: number
    renewalDate: string
    dateBought: string
    textField: string
    monthsPerRenewal: number
}

// Primary Component
export const ProgramOverviewPage: React.SFC<IProgramOverviewPageProps> = props => {
    const {history, match} = props

    const {
        loginContextVariables: {accessToken, refreshToken, isAdmin},
    } = useContext(LoginContext)

    const axios = new AxiosService(accessToken, refreshToken)
    const [img, setImg] = useState('')
    const [programData, setProgramData] = useState<ExpectedProgramOverview>({
        countProgInUse: 0,
        countProgOverall: 0,
        icon: '',
        isCostPerYear: false,
        progCostPerYear: 0,
        progFlatCost: 0,
        program: '',
        programlicenseKey: '',
    })
    const [programRows, setProgramRows] = useState<ITableItem[][]>([])
    const [pluginRows, setPluginRows] = useState<ITableItem[][]>([])

    const programHeaders = [`${match.params.id}`, 'Employee', 'License Key', 'Renewal Date']
    const pluginHeaders = ['Plugins', 'Renewal Date', 'Cost']

    useEffect(() => {
        axios
            .get(`/detail/ProgramOverview/${match.params.id}`)
            .then((data: any) => {
                setProgramData(data[0].programOverview)
                let prog: ITableItem[][] = []
                data[0].inDivPrograms.map((i: ExpectedProgramType) =>
                    prog.push(
                        i.employeeName
                            ? [
                                  {value: i.programId, id: i.programId, sortBy: i.programId, onClick: handleCopyClick},
                                  {
                                      value: format(i.employeeName),
                                      id: i.employeeId,
                                      sortBy: format(i.employeeName),
                                      onClick: handleEmpClick,
                                  },
                                  {value: format(i.programlicenseKey), sortBy: i.programlicenseKey},
                                  {value: formatDate(i.renewalDate), sortBy: formatDate(i.renewalDate)},
                              ]
                            : [
                                  {value: i.programId, id: i.programId, sortBy: i.programId, onClick: handleCopyClick},
                                  {
                                      value: format(i.employeeName),
                                      id: i.employeeId,
                                      sortBy: format(i.employeeName),
                                  },
                                  {value: format(i.programlicenseKey), sortBy: i.programlicenseKey},
                                  {value: formatDate(i.renewalDate), sortBy: formatDate(i.renewalDate)},
                              ]
                    )
                )
                setProgramRows(prog)

                let plug: ITableItem[][] = []
                data[0].listOfPlugins.map((i: ExpectedPluginType) =>
                    plug.push([
                        {value: format(i.pluginName), sortBy: format(i.pluginName), tooltip: i.textField},
                        {value: formatDate(i.renewalDate), sortBy: formatDate(i.renewalDate)},
                        {
                            value: formatCost(i.isCostPerYear, i.pluginCostPerYear, i.pluginFlatCost),
                            sortBy: i.pluginCostPerYear,
                        },
                    ])
                )
                setPluginRows(plug)
            })
            .catch((err: any) => console.error(err))
    }, [])

    useEffect(() => {
        //once icon has a value, check to see if that picture exists. If it doesnt then use the placeholder
        if (programData.icon !== '') {
            axios
                .get(programData.icon)
                .then((data: any) => {
                    if (data !== '') {
                        setImg(URL + programData.icon)
                    } else {
                        setImg(placeholder)
                    }
                })
                .catch((err: any) => console.error(err))
        } else {
            setImg('')
        }
    }, [programData.icon])

    const handleEmpClick = (id: number) => {
        history.push(`/employees/${id}`)
    }

    const handleCopyClick = (id: number) => {
        history.push(`/programs/details/${id}`)
    }

    const handleArchive = () => {
        //cant archive everything unless there are no plugins
        if (pluginRows.length > 0) {
            window.alert('Please archive the plugins before you archive this program.')
        } else {
            if (
                window.confirm(
                    `Are you sure you want to archive all copies of ${match.params.id}? ${programData.countProgInUse} are in use.`
                )
            ) {
                programRows.forEach(program => {
                    axios.put(`archive/program/${program[0].id}`, {}).catch((err: any) => console.error(err))
                })
                setProgramRows([])
                history.push('/programs')
            }
        }
    }

    return (
        <div className={styles.progOverviewMain}>
            <div className={styles.columns}>
                {/* column 1 */}
                <div className={styles.firstColumn}>
                    <Button
                        text='All Programs'
                        icon='back'
                        onClick={() => {
                            history.push('/programs')
                        }}
                        className={styles.backButton}
                        textClassName={styles.backButtonText}
                    />
                    <div className={styles.imgPadding}>
                        <img className={styles.img} src={img} alt={''} />
                    </div>
                    <div className={styles.costText}>
                        {programData.progFlatCost > 0 && (
                            <Group>
                                <p>Paid</p>
                                <div className={styles.costLine} />
                                <p>${programData.progFlatCost} </p>
                            </Group>
                        )}
                        {programData.isCostPerYear ? (
                            <Group>
                                <p>Yearly</p>
                                <div className={styles.costLine} />
                                <p>${programData.progCostPerYear} </p>
                            </Group>
                        ) : (
                            programData.progCostPerYear > 0 && (
                                <Group>
                                    <p>Monthly</p>
                                    <div className={styles.costLine} />
                                    <p>${programData.progCostPerYear} </p>
                                </Group>
                            )
                        )}
                    </div>
                </div>
                {/* column 2 */}
                <div className={styles.secondColumn}>
                    {isAdmin && (
                        <Group direction='row' justify='start' className={styles.group}>
                            <Button
                                text='Edit'
                                icon='edit'
                                onClick={() => {
                                    history.push('/programs/edit/overview/' + match.params.id)
                                }}
                                className={styles.editbutton}
                            />

                            <Button
                                text='Archive'
                                icon='archive'
                                onClick={handleArchive}
                                className={styles.archivebutton}
                            />
                        </Group>
                    )}
                    <div className={styles.titleText}>
                        <div className={styles.programName}>{match.params.id}</div>
                        <div className={styles.programText}>
                            {programData.countProgInUse} / {programData.countProgOverall} Used
                        </div>
                        {programData.programlicenseKey && (
                            <div className={styles.programText}>License Key: {programData.programlicenseKey}</div>
                        )}
                    </div>
                    <DetailPageTable headers={programHeaders} rows={programRows} setRows={setProgramRows} />
                    <div className={styles.spaceBetweenTables} />
                    <DetailPageTable headers={pluginHeaders} rows={pluginRows} setRows={setPluginRows} />
                </div>
            </div>
        </div>
    )
}